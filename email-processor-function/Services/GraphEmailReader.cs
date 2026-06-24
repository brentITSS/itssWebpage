using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Azure.Storage.Blobs;
using Azure.Identity;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using email_processor_function.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.Messages.Item.Move;
using Microsoft.Graph.Users.Item.SendMail;
using MimeKit;
using MsgReader.Outlook;
using UglyToad.PdfPig;

namespace email_processor_function.Services;

public interface IGraphEmailReader
{
    Task<ProcessPropertyHubEmailsResponse> ReadPropertyHubFolderAsync(
        ProcessPropertyHubEmailsRequest request,
        CancellationToken cancellationToken = default);
}

public class GraphEmailReader : IGraphEmailReader
{
    private const string CompletedCategory = "Completed";
    private static readonly HttpClient FxHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };
    private readonly IConfiguration _configuration;
    private readonly IOpenAiWorkflowService _openAi;
    private readonly ILogger<GraphEmailReader> _logger;

    public GraphEmailReader(
        IConfiguration configuration,
        IOpenAiWorkflowService openAi,
        ILogger<GraphEmailReader> logger)
    {
        _configuration = configuration;
        _openAi = openAi;
        _logger = logger;
    }

    public async Task<ProcessPropertyHubEmailsResponse> ReadPropertyHubFolderAsync(
        ProcessPropertyHubEmailsRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetRequired("Graph:TenantId", "Graph__TenantId");
        var clientId = GetRequired("Graph:ClientId", "Graph__ClientId");
        var clientSecret = GetRequired("Graph:ClientSecret", "Graph__ClientSecret");
        var configuredMailbox = GetRequired("Graph:MailboxUser", "Graph__MailboxUser");
        var allowedDomain = _configuration["Graph:AllowedDomain"] ??
                            Environment.GetEnvironmentVariable("Graph__AllowedDomain") ??
                            "itsystemsolutions.co.uk";

        var mailboxUser = (request.MailboxUser ?? configuredMailbox).Trim();
        if (!mailboxUser.EndsWith($"@{allowedDomain}", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Mailbox user '{mailboxUser}' is not allowed. Expected domain: @{allowedDomain}");
        }

        var maxEmails = Math.Clamp(request.MaxEmails ?? 20, 1, 100);
        var templates = await LoadClassificationTemplatesAsync(cancellationToken);
        var workflowRules = await LoadWorkflowRulesAsync(cancellationToken);

        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        var graphClient = new GraphServiceClient(credential, new[] { "https://graph.microsoft.com/.default" });

        _logger.LogInformation("Reading Inbox/Property Hub for mailbox {MailboxUser}", mailboxUser);

        var folders = await graphClient.Users[mailboxUser]
            .MailFolders["Inbox"]
            .ChildFolders
            .GetAsync(cfg =>
            {
                cfg.QueryParameters.Filter = "displayName eq 'Property Hub'";
                cfg.QueryParameters.Select = new[] { "id", "displayName", "totalItemCount", "unreadItemCount" };
                cfg.QueryParameters.Top = 1;
            }, cancellationToken);

        var propertyHubFolder = folders?.Value?.FirstOrDefault();
        if (propertyHubFolder?.Id == null)
        {
            throw new InvalidOperationException("Could not find folder 'Inbox/Property Hub' for mailbox.");
        }

        var messagesPage = await graphClient.Users[mailboxUser]
            .MailFolders[propertyHubFolder.Id]
            .Messages
            .GetAsync(cfg =>
            {
                cfg.QueryParameters.Top = maxEmails;
                cfg.QueryParameters.Orderby = new[] { "receivedDateTime desc" };
                cfg.QueryParameters.Select = new[] { "id", "subject", "from", "receivedDateTime", "hasAttachments", "categories", "flag" };
                // OData: combining flag/flagStatus filter with receivedDateTime order triggers InefficientFilter on Exchange.
                // Skip legacy "Completed" category in $filter only; exclude flag-complete in-memory below.
                cfg.QueryParameters.Filter = $"not(categories/any(c:c eq '{CompletedCategory}'))";
            }, cancellationToken);

        var allFetched = messagesPage?.Value ?? new List<Message>();
        // Extra in-code guard in case mailbox categories are case-variant or filter behavior differs.
        var eligibleMessages = allFetched
            .Where(x => !(x.Categories?.Any(c => c.Equals(CompletedCategory, StringComparison.OrdinalIgnoreCase)) ?? false))
            .Where(x => x.Flag?.FlagStatus != FollowupFlagStatus.Complete)
            .ToList();

        var processedPreviews = new List<EmailMessagePreview>();
        var unclassifiedCount = 0;
        foreach (var message in eligibleMessages)
        {
            var detailedMessage = await graphClient.Users[mailboxUser]
                .Messages[message.Id!]
                .GetAsync(cfg =>
                {
                    cfg.QueryParameters.Select =
                        new[] { "id", "subject", "from", "receivedDateTime", "hasAttachments", "body", "categories", "flag" };
                    cfg.QueryParameters.Expand = new[] { "attachments($select=id,name,contentType,size)" };
                }, cancellationToken);

            if (detailedMessage == null)
            {
                continue;
            }

            var cleanedBody = CleanupEmailBody(detailedMessage.Body?.Content ?? string.Empty);
            var attachmentExtraction = await ExtractAttachmentContentAsync(graphClient, mailboxUser, detailedMessage, cancellationToken);
            var consolidatedContent = BuildConsolidatedContent(
                detailedMessage.Subject ?? string.Empty,
                cleanedBody,
                attachmentExtraction.ExtractedTextChunks);

            var classification = await ClassifyDocumentAsync(consolidatedContent, templates, cancellationToken);
            if (classification.Label.Equals("Unclassified", StringComparison.OrdinalIgnoreCase))
            {
                unclassifiedCount++;
            }

            var (postActions, workflowPreview) = await ExecuteWorkflowAsync(
                graphClient,
                mailboxUser,
                propertyHubFolder.Id,
                detailedMessage,
                attachmentExtraction.Attachments,
                consolidatedContent,
                attachmentExtraction.ExtractionDocumentsBundled,
                classification.Label,
                classification.Score,
                classification.Source,
                workflowRules,
                cancellationToken);

            processedPreviews.Add(new EmailMessagePreview
            {
                MessageId = detailedMessage.Id ?? string.Empty,
                Subject = detailedMessage.Subject ?? "(no subject)",
                From = detailedMessage.From?.EmailAddress?.Address,
                ReceivedDateTime = detailedMessage.ReceivedDateTime,
                HasAttachments = detailedMessage.HasAttachments ?? false,
                Categories = detailedMessage.Categories?.ToList() ?? new List<string>(),
                ProcessingStatus = postActions,
                ClassificationLabel = classification.Label,
                ClassificationScore = classification.Score,
                ClassificationExplainability = classification.Explainability,
                ClassificationSource = classification.Source,
                Attachments = attachmentExtraction.Attachments,
                WorkflowContextPreview = workflowPreview
            });
        }

        var response = new ProcessPropertyHubEmailsResponse
        {
            MailboxUser = mailboxUser,
            FolderTotalItemCount = propertyHubFolder.TotalItemCount ?? 0,
            FolderUnreadItemCount = propertyHubFolder.UnreadItemCount ?? 0,
            EligibleCount = eligibleMessages.Count,
            ProcessedCount = processedPreviews.Count,
            SkippedCompletedCount = allFetched.Count - eligibleMessages.Count,
            UnclassifiedCount = unclassifiedCount,
            Messages = processedPreviews
        };
        return response;
    }

    private async Task<List<ClassificationTemplate>> LoadClassificationTemplatesAsync(CancellationToken cancellationToken)
    {
        var connectionString = GetRequired("ConnectionStrings:DefaultConnection", "ConnectionStrings__DefaultConnection");
        var results = new List<ClassificationTemplate>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                l.DocumentClassificationLabelId,
                l.ClassificationLabel,
                l.ClassificationDescription,
                l.ClassificationPrompt
            FROM tbldocumentclassificationlabel l
            INNER JOIN tbldocumentlabelset s
                ON s.DocumentLabelSetId = l.DocumentLabelSetId
            WHERE l.IsActive = 1
              AND s.IsActive = 1
            """;

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new ClassificationTemplate
            {
                DocumentClassificationLabelId = reader.GetInt32(0),
                ClassificationLabel = reader.GetString(1),
                ClassificationDescription = reader.IsDBNull(2) ? null : reader.GetString(2),
                ClassificationPrompt = reader.GetString(3)
            });
        }

        return results;
    }

    private async Task<List<WorkflowRule>> LoadWorkflowRulesAsync(CancellationToken cancellationToken)
    {
        var connectionString = GetRequired("ConnectionStrings:DefaultConnection", "ConnectionStrings__DefaultConnection");
        var rules = new Dictionary<int, WorkflowRule>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                r.DocumentWorkflowRuleId,
                r.WorkflowName,
                r.ClassificationLabel,
                r.MinimumScore,
                r.Priority,
                r.StopOnFailure,
                r.IsActive,
                s.StepOrder,
                s.StepType,
                s.StepConfigJson,
                s.IsActive
            FROM tbldocumentworkflowrule r
            LEFT JOIN tbldocumentworkflowstep s
                ON s.DocumentWorkflowRuleId = r.DocumentWorkflowRuleId
            WHERE r.IsActive = 1
            ORDER BY r.Priority ASC, r.DocumentWorkflowRuleId ASC, s.StepOrder ASC
            """;

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var ruleId = reader.GetInt32(0);
            if (!rules.TryGetValue(ruleId, out var rule))
            {
                rule = new WorkflowRule
                {
                    DocumentWorkflowRuleId = ruleId,
                    WorkflowName = reader.GetString(1),
                    ClassificationLabel = reader.GetString(2),
                    MinimumScore = reader.IsDBNull(3) ? 0.28 : reader.GetDouble(3),
                    Priority = reader.IsDBNull(4) ? 100 : reader.GetInt32(4),
                    StopOnFailure = !reader.IsDBNull(5) && reader.GetBoolean(5),
                    IsActive = !reader.IsDBNull(6) && reader.GetBoolean(6)
                };
                rules[ruleId] = rule;
            }

            if (!reader.IsDBNull(7) && !reader.IsDBNull(8) && !reader.IsDBNull(10) && reader.GetBoolean(10))
            {
                rule.Steps.Add(new WorkflowStep
                {
                    StepOrder = reader.GetInt32(7),
                    StepType = reader.GetString(8),
                    StepConfigJson = reader.IsDBNull(9) ? null : reader.GetString(9)
                });
            }
        }

        return rules.Values.ToList();
    }

    private async Task<(string ProcessingStatus, Dictionary<string, string>? WorkflowContextPreview)> ExecuteWorkflowAsync(
        GraphServiceClient graphClient,
        string mailboxUser,
        string propertyHubFolderId,
        Message message,
        List<AttachmentPreview> attachmentPreviews,
        string consolidatedContent,
        string extractionDocumentsBundled,
        string classificationLabel,
        double classificationScore,
        string classificationSource,
        List<WorkflowRule> rules,
        CancellationToken cancellationToken)
    {
        if (message.Id == null)
        {
            return ("processed", null);
        }

        var matchedRule = rules
            .Where(r =>
                r.IsActive &&
                classificationScore >= r.MinimumScore &&
                string.Equals(r.ClassificationLabel, classificationLabel, StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.DocumentWorkflowRuleId)
            .FirstOrDefault();

        // Always ensure category reflects the final classification label.
        await ApplyCategoryAsync(graphClient, mailboxUser, message.Id, message.Categories, classificationLabel, null, cancellationToken);
        var workflowContext = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        long? auditRunId = null;
        SqlConnection? auditConnection = null;
        string? auditUnavailableReason = null;

        try
        {
            var connectionString = GetRequired("ConnectionStrings:DefaultConnection", "ConnectionStrings__DefaultConnection");
            auditConnection = new SqlConnection(connectionString);
            await auditConnection.OpenAsync(cancellationToken);
            await EnsureWorkflowAuditTablesAsync(auditConnection, cancellationToken);
            auditRunId = await InsertWorkflowAuditRunAsync(
                auditConnection,
                message,
                mailboxUser,
                classificationLabel,
                classificationScore,
                matchedRule,
                matchedRule == null ? "NoRuleMatched" : "Started",
                cancellationToken);
        }
        catch (Exception auditEx)
        {
            _logger.LogWarning(auditEx, "Workflow audit setup failed for message {MessageId}. Continuing without audit row.", message.Id);
            auditUnavailableReason = auditEx.Message;
        }

        try
        {
            if (matchedRule == null || matchedRule.Steps.Count == 0)
            {
                if (auditConnection != null && auditRunId.HasValue)
                {
                    await FinalizeWorkflowAuditRunAsync(
                        auditConnection,
                        auditRunId.Value,
                        "NoRuleMatched",
                        null,
                        null,
                        null,
                        cancellationToken);
                }
                return (
                    string.IsNullOrWhiteSpace(auditUnavailableReason)
                        ? "processed"
                        : $"processed|audit_unavailable:{auditUnavailableReason}",
                    null);
            }

            workflowContext["workflowMeta_classificationSource"] = classificationSource;

            var currentMessageId = message.Id;
            foreach (var step in matchedRule.Steps.OrderBy(s => s.StepOrder))
            {
                long? auditStepId = null;
                try
                {
                    if (auditConnection != null && auditRunId.HasValue)
                    {
                        auditStepId = await InsertWorkflowAuditStepAsync(
                            auditConnection,
                            auditRunId.Value,
                            step,
                            "Started",
                            null,
                            cancellationToken);
                    }

                    var stepType = step.StepType.Trim().ToLowerInvariant();
                    if (stepType == "setcategory")
                    {
                        var category = classificationLabel;
                    string? categoryColor = null;
                        if (!string.IsNullOrWhiteSpace(step.StepConfigJson))
                        {
                            using var config = JsonDocument.Parse(step.StepConfigJson);
                            if (config.RootElement.TryGetProperty("category", out var categoryEl))
                            {
                                category = categoryEl.GetString()?.Trim() ?? category;
                            }
                        if (config.RootElement.TryGetProperty("categoryColor", out var categoryColorEl))
                        {
                            categoryColor = categoryColorEl.GetString()?.Trim();
                        }
                        }
                    await ApplyCategoryAsync(graphClient, mailboxUser, currentMessageId, null, category, categoryColor, cancellationToken);
                    }
                    else if (stepType == "markcompleted")
                    {
                        // Outlook "Mark Complete" follows the native follow-up flag, not an Outlook Category named "Completed".
                        await MarkMessageFlagCompleteAsync(graphClient, mailboxUser, currentMessageId, cancellationToken);
                    }
                    else if (stepType == "movetofolder")
                    {
                        var destinationPath = "Inbox/Property Hub";
                        if (!string.IsNullOrWhiteSpace(step.StepConfigJson))
                        {
                            using var config = JsonDocument.Parse(step.StepConfigJson);
                            if (config.RootElement.TryGetProperty("destinationPath", out var pathEl))
                            {
                                destinationPath = pathEl.GetString()?.Trim() ?? destinationPath;
                            }
                        }

                        var destinationId = await ResolveFolderIdByPathAsync(
                            graphClient,
                            mailboxUser,
                            destinationPath,
                            propertyHubFolderId,
                            cancellationToken);

                        var moveRequest = new MovePostRequestBody
                        {
                            DestinationId = destinationId
                        };
                        var moved = await graphClient.Users[mailboxUser]
                            .Messages[currentMessageId]
                            .Move
                            .PostAsync(moveRequest, cancellationToken: cancellationToken);
                        if (!string.IsNullOrWhiteSpace(moved?.Id))
                        {
                            currentMessageId = moved.Id;
                        }
                    }
                    else if (stepType == "createjournallog")
                    {
                        await CreateJournalLogAsync(
                            message,
                            attachmentPreviews,
                            classificationLabel,
                            classificationScore,
                            step.StepConfigJson,
                            workflowContext,
                            cancellationToken);
                    }
                    else if (stepType == "createcontactlog")
                    {
                        await CreateContactLogAsync(
                            message,
                            attachmentPreviews,
                            classificationLabel,
                            classificationScore,
                            step.StepConfigJson,
                            workflowContext,
                            cancellationToken);
                    }
                    else if (stepType == "runextraction")
                    {
                        var useAttachmentBundle = !string.IsNullOrWhiteSpace(extractionDocumentsBundled);
                        var extractionCorpus = useAttachmentBundle
                            ? extractionDocumentsBundled!
                            : consolidatedContent;
                        if (!useAttachmentBundle)
                        {
                            _logger.LogInformation(
                                "RunExtraction: no extractable attachment text; using subject+body envelope as fallback.");
                        }

                        await RunExtractionStepAsync(
                            extractionCorpus,
                            step.StepConfigJson,
                            workflowContext,
                            cancellationToken,
                            useAttachmentBundle ? "attachments_bundle" : "full_email_fallback");
                    }
                    else if (stepType == "runsummarisation")
                    {
                        await RunSummarisationStepAsync(
                            consolidatedContent,
                            step.StepConfigJson,
                            workflowContext,
                            cancellationToken);
                    }
                    else if (stepType == "sendemail")
                    {
                        await SendWorkflowEmailAsync(
                            graphClient,
                            mailboxUser,
                            message,
                            classificationLabel,
                            classificationScore,
                            step.StepConfigJson,
                            workflowContext,
                            cancellationToken);
                    }

                    if (auditConnection != null && auditStepId.HasValue)
                    {
                        await FinalizeWorkflowAuditStepAsync(
                            auditConnection,
                            auditStepId.Value,
                            "Completed",
                            null,
                            cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Workflow step {StepType} failed for message {MessageId}", step.StepType, currentMessageId);
                    if (auditConnection != null && auditStepId.HasValue)
                    {
                        await FinalizeWorkflowAuditStepAsync(
                            auditConnection,
                            auditStepId.Value,
                            "Failed",
                            ex.Message,
                            cancellationToken);
                    }
                    if (matchedRule.StopOnFailure)
                    {
                        if (auditConnection != null && auditRunId.HasValue)
                        {
                            await FinalizeWorkflowAuditRunWithSnapshotsAsync(
                                auditConnection,
                                auditRunId.Value,
                                "Failed",
                                $"{step.StepType}: {ex.Message}",
                                workflowContext,
                                cancellationToken);
                        }
                        var safeError = SanitizeStatusSegment(ex.Message);
                        var baseStatus = $"workflow_failed:{step.StepType}:{safeError}";
                        return (
                            string.IsNullOrWhiteSpace(auditUnavailableReason)
                                ? baseStatus
                                : $"{baseStatus}|audit_unavailable:{SanitizeStatusSegment(auditUnavailableReason)}",
                            CloneWorkflowContextForPreview(workflowContext));
                    }
                }
            }

            if (auditConnection != null && auditRunId.HasValue)
            {
                await FinalizeWorkflowAuditRunWithSnapshotsAsync(
                    auditConnection,
                    auditRunId.Value,
                    "Completed",
                    null,
                    workflowContext,
                    cancellationToken);
            }

            return (
                string.IsNullOrWhiteSpace(auditUnavailableReason)
                    ? "workflow_applied"
                    : $"workflow_applied|audit_unavailable:{auditUnavailableReason}",
                CloneWorkflowContextForPreview(workflowContext));
        }
        finally
        {
            if (auditConnection != null)
            {
                await auditConnection.DisposeAsync();
            }
        }
    }

    private static async Task EnsureWorkflowAuditTablesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbldocumentworkflowauditrun')
            BEGIN
                CREATE TABLE tbldocumentworkflowauditrun (
                    DocumentWorkflowAuditRunId BIGINT IDENTITY(1,1) PRIMARY KEY,
                    MessageId NVARCHAR(1024) NOT NULL,
                    MailboxUser NVARCHAR(320) NULL,
                    Subject NVARCHAR(500) NULL,
                    ClassificationLabel NVARCHAR(120) NULL,
                    ClassificationScore FLOAT NULL,
                    DocumentWorkflowRuleId INT NULL,
                    WorkflowName NVARCHAR(200) NULL,
                    Status NVARCHAR(40) NOT NULL,
                    ErrorMessage NVARCHAR(MAX) NULL,
                    SummarisationText NVARCHAR(MAX) NULL,
                    ExtractionJson NVARCHAR(MAX) NULL,
                    StartedDate DATETIME2 NOT NULL CONSTRAINT DF_tbldocumentworkflowauditrun_StartedDate DEFAULT (SYSUTCDATETIME()),
                    CompletedDate DATETIME2 NULL
                );
                CREATE INDEX IX_tbldocumentworkflowauditrun_MessageId
                    ON tbldocumentworkflowauditrun (MessageId, StartedDate DESC);
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbldocumentworkflowauditstep')
            BEGIN
                CREATE TABLE tbldocumentworkflowauditstep (
                    DocumentWorkflowAuditStepId BIGINT IDENTITY(1,1) PRIMARY KEY,
                    DocumentWorkflowAuditRunId BIGINT NOT NULL,
                    StepOrder INT NOT NULL,
                    StepType NVARCHAR(80) NOT NULL,
                    Status NVARCHAR(40) NOT NULL,
                    Details NVARCHAR(MAX) NULL,
                    StartedDate DATETIME2 NOT NULL CONSTRAINT DF_tbldocumentworkflowauditstep_StartedDate DEFAULT (SYSUTCDATETIME()),
                    CompletedDate DATETIME2 NULL,
                    CONSTRAINT FK_tbldocumentworkflowauditstep_tbldocumentworkflowauditrun
                        FOREIGN KEY (DocumentWorkflowAuditRunId)
                        REFERENCES tbldocumentworkflowauditrun(DocumentWorkflowAuditRunId)
                );
                CREATE INDEX IX_tbldocumentworkflowauditstep_RunId_Order
                    ON tbldocumentworkflowauditstep (DocumentWorkflowAuditRunId, StepOrder);
            END;

            IF COL_LENGTH('dbo.tbldocumentworkflowauditrun', 'SummarisationText') IS NULL
                ALTER TABLE dbo.tbldocumentworkflowauditrun ADD SummarisationText NVARCHAR(MAX) NULL;

            IF COL_LENGTH('dbo.tbldocumentworkflowauditrun', 'ExtractionJson') IS NULL
                ALTER TABLE dbo.tbldocumentworkflowauditrun ADD ExtractionJson NVARCHAR(MAX) NULL;

            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbldocumentworkflowextractionsnapshot')
            BEGIN
                CREATE TABLE tbldocumentworkflowextractionsnapshot (
                    DocumentWorkflowExtractionSnapshotId BIGINT IDENTITY(1,1) PRIMARY KEY,
                    DocumentWorkflowAuditRunId BIGINT NOT NULL,
                    FieldName NVARCHAR(200) NOT NULL,
                    FieldValue NVARCHAR(MAX) NULL,
                    Comments NVARCHAR(MAX) NULL,
                    CONSTRAINT FK_tbldocumentworkflowextractionsnapshot_tbldocumentworkflowauditrun
                        FOREIGN KEY (DocumentWorkflowAuditRunId)
                        REFERENCES tbldocumentworkflowauditrun(DocumentWorkflowAuditRunId)
                );
                CREATE INDEX IX_tbldocumentworkflowextractionsnapshot_AuditRunId
                    ON tbldocumentworkflowextractionsnapshot (DocumentWorkflowAuditRunId);
                CREATE INDEX IX_tbldocumentworkflowextractionsnapshot_FieldName
                    ON tbldocumentworkflowextractionsnapshot (FieldName);
            END;
            """;

        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<long?> InsertWorkflowAuditRunAsync(
        SqlConnection connection,
        Message message,
        string mailboxUser,
        string classificationLabel,
        double classificationScore,
        WorkflowRule? matchedRule,
        string status,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO tbldocumentworkflowauditrun (
                MessageId,
                MailboxUser,
                Subject,
                ClassificationLabel,
                ClassificationScore,
                DocumentWorkflowRuleId,
                WorkflowName,
                Status
            )
            VALUES (
                @messageId,
                @mailboxUser,
                @subject,
                @classificationLabel,
                @classificationScore,
                @documentWorkflowRuleId,
                @workflowName,
                @status
            );
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@messageId", message.Id ?? string.Empty);
        command.Parameters.AddWithValue("@mailboxUser", mailboxUser);
        command.Parameters.AddWithValue("@subject", (object?)message.Subject ?? DBNull.Value);
        command.Parameters.AddWithValue("@classificationLabel", classificationLabel);
        command.Parameters.AddWithValue("@classificationScore", classificationScore);
        command.Parameters.AddWithValue("@documentWorkflowRuleId", (object?)matchedRule?.DocumentWorkflowRuleId ?? DBNull.Value);
        command.Parameters.AddWithValue("@workflowName", (object?)matchedRule?.WorkflowName ?? DBNull.Value);
        command.Parameters.AddWithValue("@status", status);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result == null || result == DBNull.Value)
        {
            return null;
        }

        return Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }

    private static async Task<long?> InsertWorkflowAuditStepAsync(
        SqlConnection connection,
        long auditRunId,
        WorkflowStep step,
        string status,
        string? details,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO tbldocumentworkflowauditstep (
                DocumentWorkflowAuditRunId,
                StepOrder,
                StepType,
                Status,
                Details
            )
            VALUES (
                @auditRunId,
                @stepOrder,
                @stepType,
                @status,
                @details
            );
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@auditRunId", auditRunId);
        command.Parameters.AddWithValue("@stepOrder", step.StepOrder);
        command.Parameters.AddWithValue("@stepType", step.StepType);
        command.Parameters.AddWithValue("@status", status);
        command.Parameters.AddWithValue("@details", (object?)details ?? DBNull.Value);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result == null || result == DBNull.Value)
        {
            return null;
        }

        return Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }

    private static async Task FinalizeWorkflowAuditRunAsync(
        SqlConnection connection,
        long auditRunId,
        string status,
        string? errorMessage,
        string? summarisationText,
        string? extractionJson,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE tbldocumentworkflowauditrun
            SET Status = @status,
                ErrorMessage = @errorMessage,
                SummarisationText = @summarisationText,
                ExtractionJson = @extractionJson,
                CompletedDate = SYSUTCDATETIME()
            WHERE DocumentWorkflowAuditRunId = @auditRunId;
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@auditRunId", auditRunId);
        command.Parameters.AddWithValue("@status", status);
        command.Parameters.AddWithValue("@errorMessage", (object?)errorMessage ?? DBNull.Value);
        command.Parameters.AddWithValue("@summarisationText", (object?)summarisationText ?? DBNull.Value);
        command.Parameters.AddWithValue("@extractionJson", (object?)extractionJson ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task FinalizeWorkflowAuditRunWithSnapshotsAsync(
        SqlConnection connection,
        long auditRunId,
        string status,
        string? errorMessage,
        IReadOnlyDictionary<string, string> workflowContext,
        CancellationToken cancellationToken)
    {
        var (summarisationText, extractionJson) = ExtractAuditOutputsFromWorkflowContext(workflowContext);
        await FinalizeWorkflowAuditRunAsync(
            connection,
            auditRunId,
            status,
            errorMessage,
            summarisationText,
            extractionJson,
            cancellationToken);
        await PersistExtractionSnapshotsAsync(connection, auditRunId, workflowContext, cancellationToken);
    }

    private static async Task PersistExtractionSnapshotsAsync(
        SqlConnection connection,
        long auditRunId,
        IReadOnlyDictionary<string, string> workflowContext,
        CancellationToken cancellationToken)
    {
        if (!workflowContext.TryGetValue("extractionJson", out var extractionJson) ||
            string.IsNullOrWhiteSpace(extractionJson))
        {
            return;
        }

        Dictionary<string, string>? extractedFields;
        try
        {
            extractedFields = JsonSerializer.Deserialize<Dictionary<string, string>>(extractionJson);
        }
        catch
        {
            return;
        }

        if (extractedFields == null || extractedFields.Count == 0)
        {
            return;
        }

        workflowContext.TryGetValue("extractionSnapshotComment", out var globalComment);
        var fieldComments = ParseExtractionSnapshotFieldComments(workflowContext);

        const string insertSql = """
            INSERT INTO tbldocumentworkflowextractionsnapshot (
                DocumentWorkflowAuditRunId,
                FieldName,
                FieldValue,
                Comments
            )
            VALUES (
                @auditRunId,
                @fieldName,
                @fieldValue,
                @comments
            );
            """;

        foreach (var field in extractedFields.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(field.Key))
            {
                continue;
            }

            var fieldName = field.Key.Trim();
            var fieldValue = field.Value?.Trim();
            string? rowComment = null;
            if (fieldComments != null &&
                fieldComments.TryGetValue(fieldName, out var perFieldComment) &&
                !string.IsNullOrWhiteSpace(perFieldComment))
            {
                rowComment = perFieldComment.Trim();
            }
            else if (!string.IsNullOrWhiteSpace(globalComment))
            {
                rowComment = globalComment.Trim();
            }

            await using var insertCommand = new SqlCommand(insertSql, connection);
            insertCommand.Parameters.AddWithValue("@auditRunId", auditRunId);
            insertCommand.Parameters.AddWithValue("@fieldName", fieldName.Length > 200 ? fieldName[..200] : fieldName);
            insertCommand.Parameters.AddWithValue("@fieldValue", (object?)fieldValue ?? DBNull.Value);
            insertCommand.Parameters.AddWithValue("@comments", (object?)rowComment ?? DBNull.Value);
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static Dictionary<string, string>? ParseExtractionSnapshotFieldComments(
        IReadOnlyDictionary<string, string> workflowContext)
    {
        if (!workflowContext.TryGetValue("extractionSnapshotFieldComments", out var raw) ||
            string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(raw);
            if (parsed == null || parsed.Count == 0)
            {
                return null;
            }

            return new Dictionary<string, string>(parsed, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return null;
        }
    }

    private static async Task FinalizeWorkflowAuditStepAsync(
        SqlConnection connection,
        long auditStepId,
        string status,
        string? details,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE tbldocumentworkflowauditstep
            SET Status = @status,
                Details = @details,
                CompletedDate = SYSUTCDATETIME()
            WHERE DocumentWorkflowAuditStepId = @auditStepId;
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@auditStepId", auditStepId);
        command.Parameters.AddWithValue("@status", status);
        command.Parameters.AddWithValue("@details", (object?)details ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task CreateJournalLogAsync(
        Message message,
        IReadOnlyList<AttachmentPreview> attachmentPreviews,
        string classificationLabel,
        double classificationScore,
        string? stepConfigJson,
        IReadOnlyDictionary<string, string> workflowContext,
        CancellationToken cancellationToken)
    {
        var connectionString = GetRequired("ConnectionStrings:DefaultConnection", "ConnectionStrings__DefaultConnection");
        var utcNow = DateTime.UtcNow;
        var effectiveDate = message.ReceivedDateTime?.UtcDateTime ?? utcNow;

        int? propertyGroupId = null;
        int? propertyId = null;
        int? tenancyId = null;
        int? tenantId = null;
        int? journalTypeId = null;
        int? journalSubTypeId = null;
        string? descriptionTemplate = null;
        string? journalReferenceTemplate = null;
        string? amountRandTemplate = null;
        string? amountGbpTemplate = null;
        string? exchangeRateTemplate = null;
        string? exchangeRateSource = null;
        int transactionDateOffsetDays = 0;
        var attachEmailAttachments = false;
        string? attachmentAddedByTemplate = null;
        var addToCalendar = false;
        int calendarDateOffsetDays = 0;
        string? calendarDateTemplate = null;
        string? calendarTitleTemplate = null;
        string? calendarNotesTemplate = null;
        var trackingDataOnly = false;
        string? tagTypeIdsCsv = null;
        string? tagTypeIdsCsvTemplate = null;

        if (!string.IsNullOrWhiteSpace(stepConfigJson))
        {
            using var config = JsonDocument.Parse(stepConfigJson);
            var root = config.RootElement;
            propertyGroupId = GetOptionalInt(root, "propertyGroupId");
            propertyId = GetOptionalInt(root, "propertyId");
            tenancyId = GetOptionalInt(root, "tenancyId");
            tenantId = GetOptionalInt(root, "tenantId");
            journalTypeId = GetOptionalInt(root, "journalTypeId");
            journalSubTypeId = GetOptionalInt(root, "journalSubTypeId");
            descriptionTemplate = GetOptionalString(root, "journalDescriptionTemplate") ?? GetOptionalString(root, "descriptionTemplate");
            journalReferenceTemplate = GetOptionalString(root, "journalReferenceTemplate");
            amountRandTemplate = GetOptionalString(root, "journalAmountRandTemplate");
            amountGbpTemplate = GetOptionalString(root, "journalAmountGbpTemplate");
            exchangeRateTemplate = GetOptionalString(root, "zarGbpCurrencyExchangeRateTemplate");
            exchangeRateSource = GetOptionalString(root, "zarGbpRateSource");
            transactionDateOffsetDays = GetOptionalInt(root, "transactionDateOffsetDays") ?? 0;
            attachEmailAttachments = GetOptionalBool(root, "attachEmailAttachments") ?? false;
            attachmentAddedByTemplate = GetOptionalString(root, "attachmentAddedByTemplate");
            addToCalendar = GetOptionalBool(root, "addToCalendar") ?? false;
            calendarDateOffsetDays = GetOptionalInt(root, "calendarDateOffsetDays") ?? 0;
            calendarDateTemplate = GetOptionalString(root, "calendarDateTemplate");
            calendarTitleTemplate = GetOptionalString(root, "calendarTitleTemplate");
            calendarNotesTemplate = GetOptionalString(root, "calendarNotesTemplate");
            trackingDataOnly = GetOptionalBool(root, "trackingDataOnly") ?? false;
            tagTypeIdsCsv = GetOptionalString(root, "tagTypeIdsCsv");
            tagTypeIdsCsvTemplate = GetOptionalString(root, "tagTypeIdsCsvTemplate");
        }

        effectiveDate = effectiveDate.AddDays(transactionDateOffsetDays);
        var renderedDescription = ApplyTemplateTokens(
            descriptionTemplate ??
            "Workflow auto-created journal log for {classificationLabel} (score {classificationScore}) from email '{subject}'.",
            message,
            classificationLabel,
            classificationScore,
            workflowContext);
        renderedDescription = WebUtility.HtmlDecode(renderedDescription).Replace('\u00A0', ' ').Trim();
        var renderedReference = ApplyTemplateTokens(journalReferenceTemplate ?? string.Empty, message, classificationLabel, classificationScore, workflowContext);
        var renderedAmountRand = ApplyTemplateTokens(amountRandTemplate ?? string.Empty, message, classificationLabel, classificationScore, workflowContext);
        var renderedAmountGbp = ApplyTemplateTokens(amountGbpTemplate ?? string.Empty, message, classificationLabel, classificationScore, workflowContext);
        var renderedExchange = ApplyTemplateTokens(exchangeRateTemplate ?? string.Empty, message, classificationLabel, classificationScore, workflowContext);
        var amountRand = ResolveJournalAmountRand(
            amountRandTemplate,
            renderedAmountRand,
            workflowContext,
            message);
        var amountGbp = ParseDecimalOrNull(renderedAmountGbp);
        var exchangeRate = ParseDecimalOrNull(renderedExchange);
        var shouldFetchLiveRate =
            string.Equals(exchangeRateSource, "live", StringComparison.OrdinalIgnoreCase) ||
            (amountRand.HasValue && !exchangeRate.HasValue);

        // Primary workflow: use live rate when configured (or when no rate value is provided).
        if (shouldFetchLiveRate)
        {
            var liveRate = await FetchLiveZarToGbpRateAsync(cancellationToken);
            if (liveRate.HasValue)
            {
                exchangeRate = liveRate;
            }
        }

        if (amountRand.HasValue && exchangeRate.HasValue && !amountGbp.HasValue)
        {
            amountGbp = Math.Round(amountRand.Value * exchangeRate.Value, 6);
        }
        else if (amountRand.HasValue && amountGbp.HasValue && !exchangeRate.HasValue && amountRand.Value != 0)
        {
            exchangeRate = Math.Round(amountGbp.Value / amountRand.Value, 10);
        }
        else if (amountGbp.HasValue && exchangeRate.HasValue && !amountRand.HasValue && exchangeRate.Value != 0)
        {
            amountRand = Math.Round(amountGbp.Value / exchangeRate.Value, 6);
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        const string insertSql = """
            INSERT INTO tblJournalLog (
                propertyGroupID,
                propertyID,
                tenancyID,
                tenantID,
                transactionDate,
                journalTypeID,
                journalSubTypeID,
                journalDescription,
                journalAmountRand,
                zAR_GBP_CurrencyExchangeRate,
                journalAmountGBP,
                journalReference,
                trackingDataOnly)
            VALUES (
                @propertyGroupId,
                @propertyId,
                @tenancyId,
                @tenantId,
                @transactionDate,
                @journalTypeId,
                @journalSubTypeId,
                @journalDescription,
                @journalAmountRand,
                @zarGbpCurrencyExchangeRate,
                @journalAmountGbp,
                @journalReference,
                @trackingDataOnly);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;
        await using var command = new SqlCommand(insertSql, connection);
        // Prefer explicit propertyGroupId from workflow config; if absent and propertyId exists, derive it from property.
        var propertyGroupIdForInsert = propertyGroupId;
        if (!propertyGroupIdForInsert.HasValue && propertyId.HasValue)
        {
            propertyGroupIdForInsert = await ResolvePropertyGroupIdAsync(connection, propertyId.Value, cancellationToken);
        }
        command.Parameters.AddWithValue("@propertyGroupId", (object?)propertyGroupIdForInsert ?? DBNull.Value);
        command.Parameters.AddWithValue("@propertyId", (object?)propertyId ?? DBNull.Value);
        command.Parameters.AddWithValue("@tenancyId", (object?)tenancyId ?? DBNull.Value);
        command.Parameters.AddWithValue("@tenantId", (object?)tenantId ?? DBNull.Value);
        command.Parameters.AddWithValue("@transactionDate", effectiveDate);
        command.Parameters.AddWithValue("@journalTypeId", (object?)journalTypeId ?? DBNull.Value);
        command.Parameters.AddWithValue("@journalSubTypeId", (object?)journalSubTypeId ?? DBNull.Value);
        command.Parameters.AddWithValue("@journalDescription", (object?)NullIfEmpty(renderedDescription) ?? DBNull.Value);
        command.Parameters.AddWithValue("@journalAmountRand", (object?)amountRand ?? DBNull.Value);
        command.Parameters.AddWithValue(
            "@zarGbpCurrencyExchangeRate",
            exchangeRate.HasValue
                ? exchangeRate.Value.ToString("0.##########", CultureInfo.InvariantCulture)
                : (object?)NullIfEmpty(renderedExchange) ?? DBNull.Value);
        command.Parameters.AddWithValue("@journalAmountGbp", (object?)amountGbp ?? DBNull.Value);
        command.Parameters.AddWithValue("@journalReference", (object?)NullIfEmpty(renderedReference) ?? DBNull.Value);
        command.Parameters.AddWithValue("@trackingDataOnly", trackingDataOnly);

        var insertedId = (int?)await command.ExecuteScalarAsync(cancellationToken);

        if (attachEmailAttachments && insertedId.HasValue && attachmentPreviews.Count > 0)
        {
            await CreateJournalAttachmentRowsAsync(
                connection,
                insertedId.Value,
                attachmentPreviews,
                ApplyTemplateTokens(attachmentAddedByTemplate ?? "Workflow", message, classificationLabel, classificationScore, workflowContext),
                cancellationToken);
        }

        if (insertedId.HasValue && addToCalendar)
        {
            var calendarDate = ResolveCalendarDate(
                effectiveDate,
                calendarDateOffsetDays,
                calendarDateTemplate,
                message,
                classificationLabel,
                classificationScore,
                workflowContext);
            await UpsertCalendarAppointmentRowAsync(
                connection,
                sourceType: "journallog",
                sourceId: insertedId.Value,
                appointmentDate: calendarDate,
                titleOverride: ApplyTemplateTokens(
                    calendarTitleTemplate ?? "Workflow journal reminder: {classificationLabel}",
                    message,
                    classificationLabel,
                    classificationScore,
                    workflowContext),
                notes: ApplyTemplateTokens(
                    calendarNotesTemplate ?? "Workflow-created journal reminder for '{subject}'.",
                    message,
                    classificationLabel,
                    classificationScore,
                    workflowContext),
                cancellationToken);
        }

        if (insertedId.HasValue)
        {
            var renderedTagTypeIdsCsv = ApplyTemplateTokens(
                tagTypeIdsCsvTemplate ?? string.Empty,
                message,
                classificationLabel,
                classificationScore,
                workflowContext);
            if (string.IsNullOrWhiteSpace(renderedTagTypeIdsCsv))
            {
                renderedTagTypeIdsCsv = tagTypeIdsCsv ?? string.Empty;
            }
            await CreateTagRowsAsync(
                connection,
                entityType: "journallog",
                entityId: insertedId.Value,
                tagTypeIdsCsv: renderedTagTypeIdsCsv,
                cancellationToken);
        }

        _logger.LogInformation(
            "Created JournalLog {JournalLogId}. Description note: {Description}",
            insertedId,
            renderedDescription);
    }

    private async Task CreateContactLogAsync(
        Message message,
        IReadOnlyList<AttachmentPreview> attachmentPreviews,
        string classificationLabel,
        double classificationScore,
        string? stepConfigJson,
        IReadOnlyDictionary<string, string> workflowContext,
        CancellationToken cancellationToken)
    {
        var connectionString = GetRequired("ConnectionStrings:DefaultConnection", "ConnectionStrings__DefaultConnection");
        var utcNow = DateTime.UtcNow;
        var effectiveDate = message.ReceivedDateTime?.UtcDateTime ?? utcNow;

        int? propertyGroupId = null;
        int? propertyId = null;
        int? tenantId = null;
        int contactLogTypeId = 0;
        var contactBy = "Workflow";
        string? notesTemplate = null;
        int contactDateOffsetDays = 0;
        var attachEmailAttachments = false;
        string? attachmentDescriptionTemplate = null;
        string? contactIdTemplate = null;
        var addToCalendar = false;
        int calendarDateOffsetDays = 0;
        string? calendarDateTemplate = null;
        string? calendarTitleTemplate = null;
        string? calendarNotesTemplate = null;
        string? tagTypeIdsCsv = null;
        string? tagTypeIdsCsvTemplate = null;

        if (!string.IsNullOrWhiteSpace(stepConfigJson))
        {
            using var config = JsonDocument.Parse(stepConfigJson);
            var root = config.RootElement;
            propertyGroupId = GetOptionalInt(root, "propertyGroupId");
            propertyId = GetOptionalInt(root, "propertyId");
            tenantId = GetOptionalInt(root, "tenantId");
            contactLogTypeId = GetOptionalInt(root, "contactLogTypeId") ?? 0;
            contactBy = GetOptionalString(root, "contactBy") ?? contactBy;
            notesTemplate = GetOptionalString(root, "notesTemplate");
            contactDateOffsetDays = GetOptionalInt(root, "contactDateOffsetDays") ?? 0;
            attachEmailAttachments = GetOptionalBool(root, "attachEmailAttachments") ?? false;
            attachmentDescriptionTemplate = GetOptionalString(root, "attachmentDescriptionTemplate");
            contactIdTemplate = GetOptionalString(root, "contactIdTemplate");
            addToCalendar = GetOptionalBool(root, "addToCalendar") ?? false;
            calendarDateOffsetDays = GetOptionalInt(root, "calendarDateOffsetDays") ?? 0;
            calendarDateTemplate = GetOptionalString(root, "calendarDateTemplate");
            calendarTitleTemplate = GetOptionalString(root, "calendarTitleTemplate");
            calendarNotesTemplate = GetOptionalString(root, "calendarNotesTemplate");
            tagTypeIdsCsv = GetOptionalString(root, "tagTypeIdsCsv");
            tagTypeIdsCsvTemplate = GetOptionalString(root, "tagTypeIdsCsvTemplate");
        }

        if (contactLogTypeId <= 0)
        {
            throw new InvalidOperationException("CreateContactLog step requires contactLogTypeId in stepConfigJson.");
        }

        effectiveDate = effectiveDate.AddDays(contactDateOffsetDays);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        const string insertSql = """
            INSERT INTO tblContactLog (propertyGrpID, propertyID, tenantID, contactDate, contactBy, contactNotes, contactLogTypeID)
            VALUES (@propertyGroupId, @propertyId, @tenantId, @contactDate, @contactBy, @contactNotes, @contactLogTypeId);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;
        await using var command = new SqlCommand(insertSql, connection);
        command.Parameters.AddWithValue("@propertyGroupId", (object?)propertyGroupId ?? DBNull.Value);
        command.Parameters.AddWithValue("@propertyId", (object?)propertyId ?? DBNull.Value);
        command.Parameters.AddWithValue("@tenantId", (object?)tenantId ?? DBNull.Value);
        command.Parameters.AddWithValue("@contactDate", effectiveDate);
        command.Parameters.AddWithValue("@contactBy", string.IsNullOrWhiteSpace(contactBy) ? "Workflow" : contactBy.Trim());
        var renderedContactNotes = ApplyTemplateTokens(
            notesTemplate ??
            "Workflow auto-created contact log for {classificationLabel} (score {classificationScore}) from '{from}' re '{subject}'.",
            message,
            classificationLabel,
            classificationScore,
            workflowContext);
        renderedContactNotes = WebUtility.HtmlDecode(renderedContactNotes).Replace('\u00A0', ' ').Trim();
        command.Parameters.AddWithValue("@contactNotes", renderedContactNotes);
        command.Parameters.AddWithValue("@contactLogTypeId", contactLogTypeId);

        var insertedId = (int?)await command.ExecuteScalarAsync(cancellationToken);

        if (attachEmailAttachments && insertedId.HasValue && attachmentPreviews.Count > 0)
        {
            await CreateContactAttachmentRowsAsync(
                connection,
                insertedId.Value,
                attachmentPreviews,
                ApplyTemplateTokens(contactIdTemplate ?? string.Empty, message, classificationLabel, classificationScore, workflowContext),
                attachmentDescriptionTemplate,
                message,
                classificationLabel,
                classificationScore,
                workflowContext,
                cancellationToken);
        }

        if (insertedId.HasValue && addToCalendar)
        {
            var calendarDate = ResolveCalendarDate(
                effectiveDate,
                calendarDateOffsetDays,
                calendarDateTemplate,
                message,
                classificationLabel,
                classificationScore,
                workflowContext);
            await UpsertCalendarAppointmentRowAsync(
                connection,
                sourceType: "contactlog",
                sourceId: insertedId.Value,
                appointmentDate: calendarDate,
                titleOverride: ApplyTemplateTokens(
                    calendarTitleTemplate ?? "Workflow contact reminder: {classificationLabel}",
                    message,
                    classificationLabel,
                    classificationScore,
                    workflowContext),
                notes: ApplyTemplateTokens(
                    calendarNotesTemplate ?? "Workflow-created contact reminder for '{subject}'.",
                    message,
                    classificationLabel,
                    classificationScore,
                    workflowContext),
                cancellationToken);
        }

        if (insertedId.HasValue)
        {
            var renderedTagTypeIdsCsv = ApplyTemplateTokens(
                tagTypeIdsCsvTemplate ?? string.Empty,
                message,
                classificationLabel,
                classificationScore,
                workflowContext);
            if (string.IsNullOrWhiteSpace(renderedTagTypeIdsCsv))
            {
                renderedTagTypeIdsCsv = tagTypeIdsCsv ?? string.Empty;
            }
            await CreateTagRowsAsync(
                connection,
                entityType: "contactlog",
                entityId: insertedId.Value,
                tagTypeIdsCsv: renderedTagTypeIdsCsv,
                cancellationToken);
        }
        _logger.LogInformation("Created ContactLog {ContactLogId} via workflow.", insertedId);
    }

    private async Task SendWorkflowEmailAsync(
        GraphServiceClient graphClient,
        string mailboxUser,
        Message message,
        string classificationLabel,
        double classificationScore,
        string? stepConfigJson,
        IReadOnlyDictionary<string, string> workflowContext,
        CancellationToken cancellationToken)
    {
        var toEmailTemplate = string.Empty;
        var subjectTemplate = "Property Hub workflow: {classificationLabel}";
        var bodyTemplate = "Workflow notification for email '{subject}'.";
        var bodyIsHtml = false;
        var saveToSentItems = true;

        if (!string.IsNullOrWhiteSpace(stepConfigJson))
        {
            using var config = JsonDocument.Parse(stepConfigJson);
            var root = config.RootElement;
            toEmailTemplate = GetOptionalString(root, "toEmailTemplate")
                ?? GetOptionalString(root, "toEmail")
                ?? string.Empty;
            subjectTemplate = GetOptionalString(root, "subjectTemplate")
                ?? GetOptionalString(root, "subject")
                ?? subjectTemplate;
            bodyTemplate = GetOptionalString(root, "bodyTemplate")
                ?? GetOptionalString(root, "body")
                ?? bodyTemplate;
            bodyIsHtml = GetOptionalBool(root, "bodyIsHtml") ?? false;
            saveToSentItems = GetOptionalBool(root, "saveToSentItems") ?? true;
        }

        var toEmail = ApplyTemplateTokens(
                toEmailTemplate,
                message,
                classificationLabel,
                classificationScore,
                workflowContext)
            .Trim();
        var subject = ApplyTemplateTokens(
                subjectTemplate,
                message,
                classificationLabel,
                classificationScore,
                workflowContext)
            .Trim();
        var body = ApplyTemplateTokens(
            bodyTemplate,
            message,
            classificationLabel,
            classificationScore,
            workflowContext);

        if (string.IsNullOrWhiteSpace(toEmail))
        {
            throw new InvalidOperationException("SendEmail step requires toEmailTemplate (or toEmail) in step config.");
        }

        if (!IsValidEmailAddress(toEmail))
        {
            throw new InvalidOperationException($"SendEmail step recipient '{toEmail}' is not a valid email address.");
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new InvalidOperationException("SendEmail step subject resolved to an empty value.");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new InvalidOperationException("SendEmail step body resolved to an empty value.");
        }

        var graphMessage = new Message
        {
            Subject = subject,
            Body = new ItemBody
            {
                ContentType = bodyIsHtml ? BodyType.Html : BodyType.Text,
                Content = body,
            },
            ToRecipients = new List<Recipient>
            {
                new()
                {
                    EmailAddress = new EmailAddress { Address = toEmail },
                },
            },
        };

        _logger.LogInformation("Sending workflow email from {MailboxUser} to {Recipient}", mailboxUser, toEmail);

        await graphClient.Users[mailboxUser].SendMail.PostAsync(
            new SendMailPostRequestBody
            {
                Message = graphMessage,
                SaveToSentItems = saveToSentItems,
            },
            cancellationToken: cancellationToken);
    }

    private static bool IsValidEmailAddress(string email)
    {
        try
        {
            _ = new System.Net.Mail.MailAddress(email);
            return email.Contains('@', StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private static async Task UpsertCalendarAppointmentRowAsync(
        SqlConnection connection,
        string sourceType,
        int sourceId,
        DateTime appointmentDate,
        string? titleOverride,
        string? notes,
        CancellationToken cancellationToken)
    {
        const string sql = """
            MERGE tblCalendarAppointment AS target
            USING (SELECT @sourceType AS sourceType, @sourceId AS sourceId) AS src
            ON target.sourceType = src.sourceType AND target.sourceID = src.sourceId
            WHEN MATCHED THEN
                UPDATE SET
                    appointmentDate = @appointmentDate,
                    isAllDay = 1,
                    titleOverride = @titleOverride,
                    notes = @notes,
                    active = 1,
                    modifiedDate = SYSUTCDATETIME()
            WHEN NOT MATCHED THEN
                INSERT (sourceType, sourceID, appointmentDate, isAllDay, titleOverride, notes, active, createdDate)
                VALUES (@sourceType, @sourceId, @appointmentDate, 1, @titleOverride, @notes, 1, SYSUTCDATETIME());
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@sourceType", sourceType);
        command.Parameters.AddWithValue("@sourceId", sourceId);
        command.Parameters.AddWithValue("@appointmentDate", appointmentDate);
        command.Parameters.AddWithValue("@titleOverride", (object?)NullIfEmpty(titleOverride) ?? DBNull.Value);
        command.Parameters.AddWithValue("@notes", (object?)NullIfEmpty(notes) ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task CreateTagRowsAsync(
        SqlConnection connection,
        string entityType,
        int entityId,
        string? tagTypeIdsCsv,
        CancellationToken cancellationToken)
    {
        var tagTypeIds = ParseIntCsv(tagTypeIdsCsv).Distinct().ToList();
        if (tagTypeIds.Count == 0)
        {
            return;
        }

        var column = entityType.Equals("contactlog", StringComparison.OrdinalIgnoreCase)
            ? "contactLogID"
            : "journalLogID";
        var otherColumn = entityType.Equals("contactlog", StringComparison.OrdinalIgnoreCase)
            ? "journalLogID"
            : "contactLogID";

        foreach (var tagTypeId in tagTypeIds)
        {
            var sql = $"""
                IF NOT EXISTS (
                    SELECT 1
                    FROM tblTagLog
                    WHERE tagTypeID = @tagTypeId
                      AND {column} = @entityId
                )
                BEGIN
                    INSERT INTO tblTagLog (tagTypeID, tagActive, {column}, {otherColumn})
                    VALUES (@tagTypeId, 1, @entityId, NULL);
                END
                """;

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@tagTypeId", tagTypeId);
            command.Parameters.AddWithValue("@entityId", entityId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static IEnumerable<int> ParseIntCsv(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            yield break;
        }

        foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) && id > 0)
            {
                yield return id;
            }
        }
    }

    private async Task RunExtractionStepAsync(
        string extractionCorpus,
        string? stepConfigJson,
        Dictionary<string, string> workflowContext,
        CancellationToken cancellationToken,
        string extractionCorpusSource)
    {
        if (string.IsNullOrWhiteSpace(stepConfigJson))
        {
            throw new InvalidOperationException("RunExtraction step requires stepConfigJson with extractionTemplateId.");
        }

        using var config = JsonDocument.Parse(stepConfigJson);
        var root = config.RootElement;
        var extractionTemplateId = GetOptionalInt(root, "extractionTemplateId");
        if (!extractionTemplateId.HasValue || extractionTemplateId <= 0)
        {
            throw new InvalidOperationException("RunExtraction step requires a valid extractionTemplateId.");
        }

        var fields = await LoadExtractionTemplateFieldsAsync(extractionTemplateId.Value, cancellationToken);
        if (fields.Count == 0)
        {
            _logger.LogInformation("RunExtraction template {TemplateId} has no active fields.", extractionTemplateId.Value);
            return;
        }

        var allowedTokens = fields
            .Select(f => NormalizeFieldToken(f.FieldName))
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var regexMap = ExtractFieldsFromContent(extractionCorpus, fields);
        var aiMap = _openAi.IsConfigured
            ? await _openAi.ExtractWithTemplateFieldsAsync(extractionCorpus, fields, allowedTokens, cancellationToken)
                .ConfigureAwait(false)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        workflowContext["workflowMeta_openAiExtractionConfigured"] = _openAi.IsConfigured ? "true" : "false";
        workflowContext["workflowMeta_openAiExtractionSuggestedKeys"] = aiMap.Count.ToString(CultureInfo.InvariantCulture);

        var extracted = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var openAiFilled = 0;
        var regexOnlyFilled = 0;
        foreach (var token in allowedTokens)
        {
            if (aiMap.TryGetValue(token, out var aiVal) && !string.IsNullOrWhiteSpace(aiVal))
            {
                extracted[token] = aiVal.Trim();
                openAiFilled++;
                continue;
            }

            if (regexMap.TryGetValue(token, out var rxVal) && !string.IsNullOrWhiteSpace(rxVal))
            {
                extracted[token] = rxVal.Trim();
                regexOnlyFilled++;
            }
        }

        foreach (var kv in extracted)
        {
            workflowContext[kv.Key] = kv.Value;
        }

        workflowContext["extractionJson"] = JsonSerializer.Serialize(extracted);
        workflowContext["workflowMeta_extractionCorpus"] = extractionCorpusSource;
        workflowContext["workflowMeta_extractionTemplateId"] =
            extractionTemplateId.Value.ToString(CultureInfo.InvariantCulture);
        workflowContext["workflowMeta_extractionFieldsDefined"] =
            fields.Count.ToString(CultureInfo.InvariantCulture);
        workflowContext["workflowMeta_extractionFieldsCaptured"] =
            extracted.Count(kv => !string.IsNullOrWhiteSpace(kv.Value)).ToString(CultureInfo.InvariantCulture);
        workflowContext["workflowMeta_openAiFilledFields"] = openAiFilled.ToString(CultureInfo.InvariantCulture);
        workflowContext["workflowMeta_regexOnlyFilledFields"] = regexOnlyFilled.ToString(CultureInfo.InvariantCulture);

        _logger.LogInformation(
            "RunExtraction template {TemplateId} captured {Captured} fields (OpenAI non-empty={Ai}, regex-only fills={RegexOnly}).",
            extractionTemplateId.Value,
            extracted.Count(kv => !string.IsNullOrWhiteSpace(kv.Value)),
            openAiFilled,
            regexOnlyFilled);
    }

    private async Task RunSummarisationStepAsync(
        string consolidatedContent,
        string? stepConfigJson,
        Dictionary<string, string> workflowContext,
        CancellationToken cancellationToken)
    {
        string? prompt = null;
        int maxSentences = 3;
        if (!string.IsNullOrWhiteSpace(stepConfigJson))
        {
            using var config = JsonDocument.Parse(stepConfigJson);
            var root = config.RootElement;
            prompt = GetOptionalString(root, "prompt");
            var templateId = GetOptionalInt(root, "summarisationTemplateId");
            if (templateId.HasValue && templateId.Value > 0 && string.IsNullOrWhiteSpace(prompt))
            {
                prompt = await LoadSummarisationTemplatePromptAsync(templateId.Value, cancellationToken);
            }

            var configuredMaxSentences = GetOptionalInt(root, "maxSentences");
            if (configuredMaxSentences.HasValue && configuredMaxSentences.Value > 0)
            {
                maxSentences = Math.Clamp(configuredMaxSentences.Value, 1, 8);
            }
        }

        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new InvalidOperationException(
                "RunSummarisation step requires stepConfigJson with summarisationTemplateId or prompt.");
        }

        string summary;
        string summarisationSource;
        if (_openAi.IsConfigured)
        {
            var aiSummary = await _openAi.SummariseDocumentAsync(consolidatedContent, prompt.Trim(), cancellationToken)
                .ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(aiSummary))
            {
                summary = aiSummary.Trim();
                summarisationSource = "openai";
            }
            else
            {
                summary = BuildHeuristicSummary(consolidatedContent, prompt, maxSentences);
                summarisationSource = "heuristic_fallback";
            }
        }
        else
        {
            summary = BuildHeuristicSummary(consolidatedContent, prompt, maxSentences);
            summarisationSource = "heuristic";
        }

        workflowContext["summary"] = summary;
        workflowContext["summarisation"] = summary;
        workflowContext["summaryPrompt"] = prompt.Trim();
        workflowContext["workflowMeta_summarisationSource"] = summarisationSource;
        _logger.LogInformation(
            "RunSummarisation source={Source} length={Length}",
            summarisationSource,
            summary.Length);
    }

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "for", "with", "from", "this", "that", "are", "was", "were", "into", "onto", "your", "their",
        "have", "has", "had", "will", "shall", "would", "could", "should", "about", "over", "under", "between", "per",
        "each", "any", "all", "not", "but", "can", "may", "might", "than", "then", "also", "such", "via"
    };

    private async Task<(string Label, double Score, string Explainability, string Source)> ClassifyDocumentAsync(
        string content,
        List<ClassificationTemplate> templates,
        CancellationToken cancellationToken)
    {
        var openAiTried = false;
        if (_openAi.IsConfigured && templates.Count > 0 && !string.IsNullOrWhiteSpace(content))
        {
            var ai = await _openAi.ClassifyWithTemplatesAsync(content, templates, cancellationToken)
                .ConfigureAwait(false);
            if (ai != null)
            {
                return (ai.Value.Label, ai.Value.Score, ai.Value.Explainability, "openai");
            }

            openAiTried = true;
        }

        var h = ClassifyAgainstTemplates(content, templates);
        return (h.Label, h.Score, h.Explainability, openAiTried ? "heuristic_fallback" : "heuristic");
    }

    private static (string Label, double Score, string Explainability) ClassifyAgainstTemplates(string content, List<ClassificationTemplate> templates)
    {
        if (templates.Count == 0 || string.IsNullOrWhiteSpace(content))
        {
            return ("Unclassified", 0, "No active classification templates or no usable content was available.");
        }

        var contentTokens = Tokenize(content);
        var normalizedContent = NormalizeForPhraseMatch(content);
        if (contentTokens.Count == 0)
        {
            return ("Unclassified", 0, "Content did not contain enough meaningful tokens for template matching.");
        }

        ClassificationTemplate? bestTemplate = null;
        double bestScore = 0d;
        double secondBestScore = 0d;
        double bestCoreScore = 0d;
        double bestLabelCoverage = 0d;
        double bestPhraseBoost = 0d;
        var bestOverlapTerms = new List<string>();

        foreach (var template in templates)
        {
            var weightedTemplateTokens = BuildWeightedTemplateTokens(template);
            if (weightedTemplateTokens.Count == 0)
            {
                continue;
            }

            var overlapTerms = weightedTemplateTokens.Keys
                .Where(contentTokens.Contains)
                .Take(10)
                .ToList();
            var matchedWeight = weightedTemplateTokens
                .Where(kv => contentTokens.Contains(kv.Key))
                .Sum(kv => kv.Value);
            var totalWeight = weightedTemplateTokens.Values.Sum();
            var coreScore = totalWeight <= 0 ? 0d : matchedWeight / totalWeight;

            var labelTokens = ExtractTokens(template.ClassificationLabel);
            var labelMatches = labelTokens.Count == 0
                ? 0
                : labelTokens.Count(contentTokens.Contains);
            var labelCoverage = labelTokens.Count == 0
                ? 0d
                : (double)labelMatches / labelTokens.Count;

            var phraseBoost = ContainsPhrase(normalizedContent, template.ClassificationLabel) ? 0.25d : 0d;

            var score = Math.Min(1d, (coreScore * 0.65d) + (labelCoverage * 0.25d) + phraseBoost);
            if (score > bestScore)
            {
                secondBestScore = bestScore;
                bestScore = score;
                bestCoreScore = coreScore;
                bestLabelCoverage = labelCoverage;
                bestPhraseBoost = phraseBoost;
                bestTemplate = template;
                bestOverlapTerms = overlapTerms;
            }
            else if (score > secondBestScore)
            {
                secondBestScore = score;
            }
        }

        var calibratedScore = CalibrateConfidence(
            bestScore,
            secondBestScore,
            bestLabelCoverage,
            bestPhraseBoost,
            bestOverlapTerms.Count);

        // Conservative threshold to avoid false matches.
        if (bestTemplate == null || calibratedScore < 0.28)
        {
            var explanation = bestTemplate == null
                ? "No template produced a meaningful lexical overlap with the email content."
                : $"Best template calibrated confidence {Math.Round(calibratedScore, 4)} is below threshold 0.28. " +
                  $"Raw={Math.Round(bestScore, 4)}, secondBest={Math.Round(secondBestScore, 4)}. " +
                  $"Closest template '{bestTemplate.ClassificationLabel}' had core={Math.Round(bestCoreScore, 4)}, " +
                  $"labelCoverage={Math.Round(bestLabelCoverage, 4)}, phraseBoost={Math.Round(bestPhraseBoost, 2)} " +
                  $"with overlap terms: {string.Join(", ", bestOverlapTerms.DefaultIfEmpty("none"))}.";
            return ("Unclassified", Math.Round(calibratedScore, 4), explanation);
        }

        var explainability =
            $"Matched template '{bestTemplate.ClassificationLabel}' with calibrated confidence {Math.Round(calibratedScore, 4)} " +
            $"(raw={Math.Round(bestScore, 4)}, secondBest={Math.Round(secondBestScore, 4)}). " +
            $"(core={Math.Round(bestCoreScore, 4)}, labelCoverage={Math.Round(bestLabelCoverage, 4)}, phraseBoost={Math.Round(bestPhraseBoost, 2)}). " +
            $"Overlap terms: {string.Join(", ", bestOverlapTerms.DefaultIfEmpty("none"))}.";

        return (bestTemplate.ClassificationLabel, Math.Round(calibratedScore, 4), explainability);
    }

    private static double CalibrateConfidence(
        double rawScore,
        double secondBestScore,
        double labelCoverage,
        double phraseBoost,
        int overlapTermCount)
    {
        var margin = Math.Max(0d, rawScore - secondBestScore);
        var calibrated = rawScore;
        calibrated += Math.Min(0.22d, margin * 1.1d);
        calibrated += Math.Min(0.18d, labelCoverage * 0.22d);
        calibrated += Math.Min(0.12d, overlapTermCount * 0.015d);
        if (phraseBoost > 0)
        {
            calibrated += 0.08d;
        }

        calibrated = Math.Min(0.995d, Math.Max(0d, calibrated));
        calibrated = 1d - Math.Pow(1d - calibrated, 1.65d);
        return Math.Min(0.995d, Math.Max(0d, calibrated));
    }

    private static Dictionary<string, double> BuildWeightedTemplateTokens(ClassificationTemplate template)
    {
        var weighted = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        AddWeightedTokens(weighted, template.ClassificationLabel, 3.0d);
        AddWeightedTokens(weighted, template.ClassificationDescription, 1.5d);
        AddWeightedTokens(weighted, template.ClassificationPrompt, 1.0d);
        return weighted;
    }

    private static void AddWeightedTokens(Dictionary<string, double> target, string? value, double weight)
    {
        foreach (var token in ExtractTokens(value))
        {
            if (!target.TryGetValue(token, out var current) || weight > current)
            {
                target[token] = weight;
            }
        }
    }

    private static List<string> ExtractTokens(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new List<string>();
        }

        return Regex.Matches(value.ToLowerInvariant(), "[a-z0-9]{3,}")
            .Select(m => m.Value)
            .Where(token => !StopWords.Contains(token))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static HashSet<string> Tokenize(string value)
    {
        return ExtractTokens(value).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool ContainsPhrase(string haystack, string phrase)
    {
        var normalizedPhrase = NormalizeForPhraseMatch(phrase);
        return normalizedPhrase.Length >= 4 && haystack.Contains(normalizedPhrase, StringComparison.Ordinal);
    }

    private static string NormalizeForPhraseMatch(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return Regex.Replace(value.ToLowerInvariant(), @"\s+", " ").Trim();
    }

    private async Task ApplyCategoryAsync(
        GraphServiceClient graphClient,
        string mailboxUser,
        string messageId,
        IEnumerable<string>? existingCategories,
        string category,
        string? categoryColor,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(category))
        {
            await EnsureMasterCategoryAsync(graphClient, mailboxUser, category, categoryColor, cancellationToken);
        }

        var categories = existingCategories?.ToList() ?? new List<string>();
        if (!categories.Any(c => c.Equals(category, StringComparison.OrdinalIgnoreCase)))
        {
            categories.Add(category);
        }

        await graphClient.Users[mailboxUser]
            .Messages[messageId]
            .PatchAsync(new Message
            {
                Categories = categories
            }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Sets the message follow-up flag to <see cref="FollowupFlagStatus.Complete"/> (Outlook ribbon "Mark Complete"),
    /// without adding an Outlook Category.
    /// </summary>
    private static async Task MarkMessageFlagCompleteAsync(
        GraphServiceClient graphClient,
        string mailboxUser,
        string messageId,
        CancellationToken cancellationToken)
    {
        var utcNow = DateTimeOffset.UtcNow;
        await graphClient.Users[mailboxUser]
            .Messages[messageId]
            .PatchAsync(
                new Message
                {
                    Flag = new FollowupFlag
                    {
                        FlagStatus = FollowupFlagStatus.Complete,
                        CompletedDateTime = new DateTimeTimeZone
                        {
                            DateTime = utcNow.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture),
                            TimeZone = "UTC"
                        }
                    }
                },
                cancellationToken: cancellationToken);
    }

    private async Task EnsureMasterCategoryAsync(
        GraphServiceClient graphClient,
        string mailboxUser,
        string categoryName,
        string? categoryColor,
        CancellationToken cancellationToken)
    {
        // Some Graph endpoints do not reliably honor OData filter here in app-only mode,
        // so we resolve category existence client-side by display name.
        var existing = await graphClient.Users[mailboxUser]
            .Outlook
            .MasterCategories
            .GetAsync(cfg =>
            {
                cfg.QueryParameters.Top = 200;
            }, cancellationToken);
        var existingCategory = existing?.Value?
            .FirstOrDefault(x => string.Equals(x.DisplayName?.Trim(), categoryName.Trim(), StringComparison.OrdinalIgnoreCase));

        var parsedColor = ParseCategoryColorOrNull(categoryColor);

        if (existingCategory == null)
        {
            var createPayload = new OutlookCategory
            {
                DisplayName = categoryName
            };
            if (parsedColor.HasValue)
            {
                createPayload.Color = parsedColor.Value;
            }

            await graphClient.Users[mailboxUser]
                .Outlook
                .MasterCategories
                .PostAsync(createPayload, cancellationToken: cancellationToken);
            return;
        }

        if (parsedColor.HasValue && existingCategory.Color != parsedColor.Value && !string.IsNullOrWhiteSpace(existingCategory.Id))
        {
            await graphClient.Users[mailboxUser]
                .Outlook
                .MasterCategories[existingCategory.Id]
                .PatchAsync(new OutlookCategory
                {
                    Color = parsedColor.Value
                }, cancellationToken: cancellationToken);
        }
    }

    private static CategoryColor? ParseCategoryColorOrNull(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return null;
        }

        if (Enum.TryParse<CategoryColor>(color, true, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static string SanitizeStatusSegment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown_error";
        }

        var singleLine = Regex.Replace(value, @"\s+", " ").Trim();
        singleLine = singleLine.Replace("|", "/", StringComparison.Ordinal);
        if (singleLine.Length > 220)
        {
            singleLine = singleLine[..220] + "...";
        }

        return singleLine;
    }

    private async Task<string> ResolveFolderIdByPathAsync(
        GraphServiceClient graphClient,
        string mailboxUser,
        string destinationPath,
        string defaultPropertyHubFolderId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            return defaultPropertyHubFolderId;
        }

        var parts = destinationPath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        if (parts.Count == 0)
        {
            return defaultPropertyHubFolderId;
        }

        string? currentFolderId = null;
        var currentStartIndex = 0;
        if (parts[0].Equals("inbox", StringComparison.OrdinalIgnoreCase))
        {
            currentFolderId = "Inbox";
            currentStartIndex = 1;
        }

        for (var i = currentStartIndex; i < parts.Count; i++)
        {
            var segment = parts[i];
            var children = currentFolderId == null
                ? await graphClient.Users[mailboxUser]
                    .MailFolders
                    .GetAsync(cfg =>
                    {
                        cfg.QueryParameters.Filter = $"displayName eq '{segment.Replace("'", "''")}'";
                        cfg.QueryParameters.Top = 1;
                    }, cancellationToken)
                : await graphClient.Users[mailboxUser]
                    .MailFolders[currentFolderId]
                    .ChildFolders
                    .GetAsync(cfg =>
                    {
                        cfg.QueryParameters.Filter = $"displayName eq '{segment.Replace("'", "''")}'";
                        cfg.QueryParameters.Top = 1;
                    }, cancellationToken);

            var next = children?.Value?.FirstOrDefault();
            if (next?.Id == null)
            {
                var createdFolder = currentFolderId == null
                    ? await graphClient.Users[mailboxUser]
                        .MailFolders
                        .PostAsync(new MailFolder
                        {
                            DisplayName = segment
                        }, cancellationToken: cancellationToken)
                    : await graphClient.Users[mailboxUser]
                        .MailFolders[currentFolderId]
                        .ChildFolders
                        .PostAsync(new MailFolder
                        {
                            DisplayName = segment
                        }, cancellationToken: cancellationToken);

                if (createdFolder?.Id == null)
                {
                    _logger.LogWarning(
                        "Failed to create destination folder segment '{Segment}' for path '{Path}'. Falling back to default folder.",
                        segment,
                        destinationPath);
                    return defaultPropertyHubFolderId;
                }

                _logger.LogInformation(
                    "Created missing destination folder segment '{Segment}' for path '{Path}'.",
                    segment,
                    destinationPath);
                currentFolderId = createdFolder.Id;
                continue;
            }

            currentFolderId = next.Id;
        }

        return currentFolderId ?? defaultPropertyHubFolderId;
    }

    private async Task<List<(string FieldName, string? ExampleValue)>> LoadExtractionTemplateFieldsAsync(
        int extractionTemplateId,
        CancellationToken cancellationToken)
    {
        var connectionString = GetRequired("ConnectionStrings:DefaultConnection", "ConnectionStrings__DefaultConnection");
        var fields = new List<(string FieldName, string? ExampleValue)>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT FieldName, ExampleValue
            FROM tbldocumentextractionfield
            WHERE DocumentExtractionTemplateId = @templateId
              AND IsActive = 1
            ORDER BY DocumentExtractionFieldId ASC
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@templateId", extractionTemplateId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            fields.Add((
                reader.GetString(0),
                reader.IsDBNull(1) ? null : reader.GetString(1)
            ));
        }

        return fields;
    }

    private static string? NullIfEmpty(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static decimal? ParseDecimalOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        var decoded = WebUtility.HtmlDecode(value).Replace('\u00A0', ' ').Trim();
        return TryEvaluateDecimalExpression(decoded, out var parsed) ? parsed : null;
    }

    private static decimal? ResolveJournalAmountRand(
        string? amountRandTemplate,
        string? renderedAmountRand,
        IReadOnlyDictionary<string, string> workflowContext,
        Message message)
    {
        var parsed = ParseDecimalOrNull(renderedAmountRand);
        if (parsed.HasValue)
        {
            return parsed;
        }

        var template = amountRandTemplate?.Trim() ?? string.Empty;
        var rendered = renderedAmountRand?.Trim() ?? string.Empty;
        var usesFieldTokens = template.Contains("{field:", StringComparison.OrdinalIgnoreCase);

        // If a template rendered some text but we still cannot parse it, fail fast with a helpful error.
        if (!string.IsNullOrWhiteSpace(template) && !string.IsNullOrWhiteSpace(rendered))
        {
            throw new InvalidOperationException(
                $"CreateJournalLog amount could not be parsed from template result '{rendered}'.");
        }

        // Attempt fallback inference so workflows without explicit amount templates can still populate value.
        var inferred = TryInferAmountFromWorkflowContext(workflowContext)
                       ?? TryInferAmountFromMessage(message);
        if (inferred.HasValue)
        {
            return inferred;
        }

        // Template references extracted fields but no value was resolved.
        // Keep workflow resilient: allow insert to continue with null amount instead of failing the whole rule.
        if (usesFieldTokens)
        {
            return null;
        }

        return null;
    }

    private static decimal? TryInferAmountFromWorkflowContext(IReadOnlyDictionary<string, string> workflowContext)
    {
        static decimal? TryParseFromValue(string? value) => ParseDecimalOrNull(value);

        var preferredKeys = new[]
        {
            "total_incl_vat", "invoice_total", "amount_due", "total_due", "statement_total",
            "amount", "journalamountrand", "total", "balance_due"
        };

        foreach (var key in preferredKeys)
        {
            if (workflowContext.TryGetValue(key, out var value))
            {
                var parsed = TryParseFromValue(value);
                if (parsed.HasValue && parsed.Value > 0)
                {
                    return parsed.Value;
                }
            }
        }

        foreach (var pair in workflowContext)
        {
            var k = pair.Key ?? string.Empty;
            if (!(k.Contains("amount", StringComparison.OrdinalIgnoreCase) ||
                  k.Contains("total", StringComparison.OrdinalIgnoreCase) ||
                  k.Contains("balance", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var parsed = TryParseFromValue(pair.Value);
            if (parsed.HasValue && parsed.Value > 0)
            {
                return parsed.Value;
            }
        }

        return null;
    }

    private static decimal? TryInferAmountFromMessage(Message message)
    {
        var body = CleanupEmailBody(message.Body?.Content ?? string.Empty);
        var subject = message.Subject ?? string.Empty;
        var text = $"{subject} {body}";
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var matches = Regex.Matches(
            text,
            @"(?ix)
            (?:\b(?:R|ZAR)\s*)?\d{1,3}(?:[ ,.]?\d{3})*(?:[.,]\d{2,4})
            |
            (?:\b(?:R|ZAR)\s*\d+(?:[.,]\d+)?)
            ");
        decimal? best = null;
        foreach (Match match in matches)
        {
            var parsed = ParseDecimalOrNull(match.Value);
            if (!parsed.HasValue || parsed.Value <= 0)
            {
                continue;
            }

            if (!best.HasValue || parsed.Value > best.Value)
            {
                best = parsed.Value;
            }
        }

        return best;
    }

    private static bool TryEvaluateDecimalExpression(string rawValue, out decimal parsed)
    {
        parsed = 0m;
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        var normalized = rawValue
            .Trim()
            .Replace('\u00A0', ' ')
            .Replace("R", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("ZAR", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("GBP", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("£", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);

        var hasComma = normalized.Contains(',', StringComparison.Ordinal);
        var hasDot = normalized.Contains('.', StringComparison.Ordinal);
        if (hasComma && hasDot)
        {
            // If comma appears after dot (e.g. 1.234,56), treat dot as thousands and comma as decimal.
            var lastComma = normalized.LastIndexOf(',');
            var lastDot = normalized.LastIndexOf('.');
            if (lastComma > lastDot)
            {
                normalized = normalized.Replace(".", string.Empty, StringComparison.Ordinal);
                normalized = normalized.Replace(",", ".", StringComparison.Ordinal);
            }
            else
            {
                // e.g. 1,234.56
                normalized = normalized.Replace(",", string.Empty, StringComparison.Ordinal);
            }
        }
        else if (hasComma)
        {
            // e.g. 1234,56
            normalized = normalized.Replace(",", ".", StringComparison.Ordinal);
        }

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        // Convert percentages so expressions like "x * 3.8%" work naturally.
        normalized = Regex.Replace(normalized, @"(\d+(?:\.\d+)?)%", "($1/100)");

        var hasOperator = normalized.IndexOfAny(new[] { '+', '-', '*', '/', '(', ')' }) >= 0;
        if (!hasOperator)
        {
            return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed);
        }

        return TryEvaluateArithmeticExpression(normalized, out parsed);
    }

    private static bool TryEvaluateArithmeticExpression(string expression, out decimal result)
    {
        result = 0m;
        var values = new Stack<decimal>();
        var operators = new Stack<char>();
        var i = 0;

        while (i < expression.Length)
        {
            var ch = expression[i];

            if (char.IsWhiteSpace(ch))
            {
                i++;
                continue;
            }

            if (ch == '(')
            {
                operators.Push(ch);
                i++;
                continue;
            }

            if (ch == ')')
            {
                while (operators.Count > 0 && operators.Peek() != '(')
                {
                    if (!ApplyOperator(values, operators.Pop()))
                    {
                        return false;
                    }
                }

                if (operators.Count == 0 || operators.Pop() != '(')
                {
                    return false;
                }

                i++;
                continue;
            }

            if (IsOperator(ch))
            {
                var isUnaryMinus = ch == '-' && (i == 0 || expression[i - 1] == '(' || IsOperator(expression[i - 1]));
                if (isUnaryMinus)
                {
                    var start = i;
                    i++;
                    while (i < expression.Length && (char.IsDigit(expression[i]) || expression[i] == '.'))
                    {
                        i++;
                    }

                    if (!decimal.TryParse(expression[start..i], NumberStyles.Any, CultureInfo.InvariantCulture, out var unaryValue))
                    {
                        return false;
                    }
                    values.Push(unaryValue);
                    continue;
                }

                while (operators.Count > 0 && operators.Peek() != '(' && Precedence(operators.Peek()) >= Precedence(ch))
                {
                    if (!ApplyOperator(values, operators.Pop()))
                    {
                        return false;
                    }
                }

                operators.Push(ch);
                i++;
                continue;
            }

            if (char.IsDigit(ch) || ch == '.')
            {
                var start = i;
                i++;
                while (i < expression.Length && (char.IsDigit(expression[i]) || expression[i] == '.'))
                {
                    i++;
                }

                if (!decimal.TryParse(expression[start..i], NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
                {
                    return false;
                }
                values.Push(number);
                continue;
            }

            return false;
        }

        while (operators.Count > 0)
        {
            if (!ApplyOperator(values, operators.Pop()))
            {
                return false;
            }
        }

        if (values.Count != 1)
        {
            return false;
        }

        result = values.Pop();
        return true;
    }

    private static bool IsOperator(char op)
    {
        return op is '+' or '-' or '*' or '/';
    }

    private static int Precedence(char op)
    {
        return op is '*' or '/' ? 2 : 1;
    }

    private static bool ApplyOperator(Stack<decimal> values, char op)
    {
        if (values.Count < 2)
        {
            return false;
        }

        var right = values.Pop();
        var left = values.Pop();
        decimal result;
        switch (op)
        {
            case '+':
                result = left + right;
                break;
            case '-':
                result = left - right;
                break;
            case '*':
                result = left * right;
                break;
            case '/':
                if (right == 0)
                {
                    return false;
                }
                result = left / right;
                break;
            default:
                return false;
        }

        values.Push(result);
        return true;
    }

    private async Task<int?> ResolvePropertyGroupIdAsync(SqlConnection connection, int propertyId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP 1 propertyGroupID
            FROM tblProperty
            WHERE propertyID = @propertyId;
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@propertyId", propertyId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result == null || result == DBNull.Value)
        {
            return null;
        }

        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    private async Task CreateJournalAttachmentRowsAsync(
        SqlConnection connection,
        int journalLogId,
        IReadOnlyList<AttachmentPreview> attachmentPreviews,
        string? attachedBy,
        CancellationToken cancellationToken)
    {
        if (attachmentPreviews.Count == 0)
        {
            return;
        }

        const string insertSql = """
            INSERT INTO tblJournalLogAttachment (journalLogID, dateAttached, attachedBy)
            VALUES (@journalLogId, @dateAttached, @attachedBy);
            """;

        foreach (var attachment in attachmentPreviews)
        {
            if (ShouldSkipWorkflowAttachment(attachment))
            {
                continue;
            }

            var resolvedFileName = ResolveWorkflowAttachmentFileName(
                attachment.Name,
                attachment.ContentType,
                attachment.ContentBytes);

            var attachedByValue = NullIfEmpty(attachedBy);
            attachedByValue = string.IsNullOrWhiteSpace(attachedByValue)
                ? $"Workflow ({resolvedFileName})"
                : $"{attachedByValue} ({resolvedFileName})";

            var blobKey = await TryUploadAttachmentToBlobAsync(
                folder: "journals/workflow",
                originalFileName: resolvedFileName,
                contentType: attachment.ContentType,
                contentBytes: attachment.ContentBytes,
                cancellationToken);
            var safeAttachedBy = BuildPersistedTextWithBlobMarker(attachedByValue, blobKey, 255);

            await using var command = new SqlCommand(insertSql, connection);
            command.Parameters.AddWithValue("@journalLogId", journalLogId);
            command.Parameters.AddWithValue("@dateAttached", DateTime.UtcNow);
            command.Parameters.AddWithValue("@attachedBy", (object?)safeAttachedBy ?? DBNull.Value);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private async Task CreateContactAttachmentRowsAsync(
        SqlConnection connection,
        int contactLogId,
        IReadOnlyList<AttachmentPreview> attachmentPreviews,
        string? contactId,
        string? attachmentDescriptionTemplate,
        Message message,
        string classificationLabel,
        double classificationScore,
        IReadOnlyDictionary<string, string> workflowContext,
        CancellationToken cancellationToken)
    {
        if (attachmentPreviews.Count == 0)
        {
            return;
        }

        const string insertSql = """
            INSERT INTO tblContactLogAttachment (contactID, contactLogID, attachmentDescription)
            VALUES (@contactId, @contactLogId, @attachmentDescription);
            """;

        foreach (var attachment in attachmentPreviews)
        {
            if (ShouldSkipWorkflowAttachment(attachment))
            {
                continue;
            }

            var resolvedFileName = ResolveWorkflowAttachmentFileName(
                attachment.Name,
                attachment.ContentType,
                attachment.ContentBytes);

            var description = ApplyTemplateTokens(
                attachmentDescriptionTemplate ?? $"Attachment: {resolvedFileName}",
                message,
                classificationLabel,
                classificationScore,
                workflowContext);

            var blobKey = await TryUploadAttachmentToBlobAsync(
                folder: "contacts/workflow",
                originalFileName: resolvedFileName,
                contentType: attachment.ContentType,
                contentBytes: attachment.ContentBytes,
                cancellationToken);
            description = BuildPersistedTextWithBlobMarker(description, blobKey, 500);

            await using var command = new SqlCommand(insertSql, connection);
            command.Parameters.AddWithValue("@contactId", (object?)NullIfEmpty(contactId) ?? DBNull.Value);
            command.Parameters.AddWithValue("@contactLogId", contactLogId);
            command.Parameters.AddWithValue("@attachmentDescription", (object?)NullIfEmpty(description) ?? DBNull.Value);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private async Task<string?> TryUploadAttachmentToBlobAsync(
        string folder,
        string? originalFileName,
        string? contentType,
        byte[]? contentBytes,
        CancellationToken cancellationToken)
    {
        if (contentBytes == null || contentBytes.Length == 0)
        {
            return null;
        }

        var connectionString =
            _configuration["AttachmentStorage:ConnectionString"] ??
            Environment.GetEnvironmentVariable("AttachmentStorage__ConnectionString");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return null;
        }

        var containerName =
            _configuration["AttachmentStorage:ContainerName"] ??
            Environment.GetEnvironmentVariable("AttachmentStorage__ContainerName") ??
            "propertyhub-attachments";
        var resolvedFileName = ResolveWorkflowAttachmentFileName(originalFileName, contentType, contentBytes);
        var extension = Path.GetExtension(resolvedFileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".bin";
        }

        // Keep blob key compact so marker always fits into DB varchar columns.
        var blobKey = $"{folder}/{DateTime.UtcNow:yyyyMMdd}/{Guid.NewGuid():N}{extension}";
        var resolvedContentType = ResolveAttachmentContentType(resolvedFileName, contentType, contentBytes);

        try
        {
            var container = new BlobContainerClient(connectionString, containerName);
            await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            var blob = container.GetBlobClient(blobKey);
            await using var stream = new MemoryStream(contentBytes, writable: false);
            await blob.UploadAsync(
                stream,
                new Azure.Storage.Blobs.Models.BlobUploadOptions
                {
                    HttpHeaders = new Azure.Storage.Blobs.Models.BlobHttpHeaders
                    {
                        ContentType = resolvedContentType,
                    },
                },
                cancellationToken);
            return blobKey;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist workflow attachment to blob storage.");
            return null;
        }
    }

    private static string? BuildPersistedTextWithBlobMarker(string? baseValue, string? blobKey, int maxLength)
    {
        var value = string.IsNullOrWhiteSpace(baseValue) ? null : baseValue.Trim();
        if (string.IsNullOrWhiteSpace(blobKey))
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Length <= maxLength ? value : value[..maxLength];
        }

        var marker = $" [blob:{blobKey}]";
        if (marker.Length >= maxLength)
        {
            return marker[..maxLength];
        }

        var prefixMax = maxLength - marker.Length;
        var prefix = string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : (value.Length <= prefixMax ? value : value[..prefixMax]);
        return $"{prefix}{marker}";
    }

    private static string SanitizeFileName(string? fileName)
    {
        var fallback = string.IsNullOrWhiteSpace(fileName) ? "attachment.bin" : fileName;
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(fallback.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "attachment.bin" : cleaned;
    }

    private static readonly Regex OutlookSpuriousAttachmentNameRegex = new(
        @"^attachment-[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static bool ShouldSkipWorkflowAttachment(AttachmentPreview attachment)
    {
        var name = attachment.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(attachment.ContentType) &&
            attachment.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (name.StartsWith("Outlook-", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("signature", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Outlook/Graph often exposes inner reference parts as attachment-{guid} with no real filename.
        if (OutlookSpuriousAttachmentNameRegex.IsMatch(name) &&
            string.IsNullOrWhiteSpace(Path.GetExtension(name)))
        {
            return true;
        }

        return false;
    }

    private static string ResolveWorkflowAttachmentFileName(string? name, string? contentType, byte[]? contentBytes)
    {
        var sanitized = SanitizeFileName(name);
        var extension = Path.GetExtension(sanitized);
        if (!string.IsNullOrWhiteSpace(extension) &&
            !extension.Equals(".bin", StringComparison.OrdinalIgnoreCase))
        {
            return sanitized;
        }

        var inferredExtension = InferExtensionFromContentType(contentType)
            ?? InferExtensionFromMagicBytes(contentBytes)
            ?? ".bin";
        var stem = Path.GetFileNameWithoutExtension(sanitized);
        if (string.IsNullOrWhiteSpace(stem))
        {
            stem = "attachment";
        }

        return $"{stem}{inferredExtension}";
    }

    private static string ResolveAttachmentContentType(string fileName, string? contentType, byte[]? contentBytes)
    {
        if (!string.IsNullOrWhiteSpace(contentType) &&
            !contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            return contentType;
        }

        var extension = Path.GetExtension(fileName);
        if (!string.IsNullOrWhiteSpace(extension))
        {
            var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
            if (provider.TryGetContentType(fileName, out var inferred))
            {
                return inferred;
            }
        }

        return InferExtensionFromMagicBytes(contentBytes) switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".gif" => "image/gif",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".txt" => "text/plain",
            _ => "application/octet-stream",
        };
    }

    private static string? InferExtensionFromContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return null;
        }

        return contentType.Trim().ToLowerInvariant() switch
        {
            "application/pdf" => ".pdf",
            "application/json" => ".json",
            "text/csv" => ".csv",
            "text/plain" => ".txt",
            "application/xml" or "text/xml" => ".xml",
            "image/png" => ".png",
            "image/jpeg" or "image/jpg" => ".jpg",
            "image/gif" => ".gif",
            "application/msword" => ".doc",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
            "application/vnd.ms-excel" => ".xls",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => ".xlsx",
            "message/rfc822" => ".eml",
            _ => null,
        };
    }

    private static string? InferExtensionFromMagicBytes(byte[]? contentBytes)
    {
        if (contentBytes == null || contentBytes.Length < 4)
        {
            return null;
        }

        if (contentBytes.Length >= 4 &&
            contentBytes[0] == 0x25 &&
            contentBytes[1] == 0x50 &&
            contentBytes[2] == 0x44 &&
            contentBytes[3] == 0x46)
        {
            return ".pdf";
        }

        if (contentBytes.Length >= 8 &&
            contentBytes[0] == 0x89 &&
            contentBytes[1] == 0x50 &&
            contentBytes[2] == 0x4E &&
            contentBytes[3] == 0x47)
        {
            return ".png";
        }

        if (contentBytes.Length >= 3 &&
            contentBytes[0] == 0xFF &&
            contentBytes[1] == 0xD8 &&
            contentBytes[2] == 0xFF)
        {
            return ".jpg";
        }

        if (contentBytes.Length >= 2 &&
            contentBytes[0] == 0x50 &&
            contentBytes[1] == 0x4B)
        {
            return ".docx";
        }

        return null;
    }

    /// <summary>Maps template keys such as total_incl_vat to PDF text variants: same string or spaced words.</summary>
    private static string AlternateLabelMatchers(string fieldNameTrimmed)
    {
        var raw = fieldNameTrimmed.Trim();
        var esc = Regex.Escape(raw);
        if (!raw.Contains('_'))
        {
            return esc;
        }

        var segs = raw.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segs.Length < 2)
        {
            return esc;
        }

        var flex = string.Join(@"[\s._\u2013\-]{0,8}", segs.Select(Regex.Escape));
        return flex == esc ? esc : $@"(?:{esc}|{flex})";
    }

    private static string BuildSiblingLabelStopAhead(string currentFieldTrimmed, IReadOnlyList<string> allSiblingNamesTrimmed)
    {
        var clauses = new List<string>();
        foreach (var s in allSiblingNamesTrimmed.Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (string.Equals(s.Trim(), currentFieldTrimmed.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var pat = AlternateLabelMatchers(s.Trim());
            clauses.Add(@$"\s+(?:{pat})\s*[:\u003a\u2013\-]");
        }

        if (clauses.Count == 0)
        {
            return @"(?=$)";
        }

        return $@"(?=(?:{string.Join("|", clauses)}|$))";
    }

    private static Dictionary<string, string> ExtractFieldsFromContent(
        string content,
        IEnumerable<(string FieldName, string? ExampleValue)> fields)
    {
        var rows = fields.ToList();
        var allNamesTrimmed = rows
            .Select(f => f.FieldName.Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var extracted = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in rows)
        {
            var normalizedName = NormalizeFieldToken(field.FieldName);
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                continue;
            }

            var rawLabel = field.FieldName.Trim();

            var strictLineRx = $@"(?im)^\s*{Regex.Escape(rawLabel)}\s*[:\u003a\u2013\-]+\s*(?<lv>[^\r\n]+)\s*$";
            var strictLm = Regex.Match(content, strictLineRx);
            if (strictLm.Success && !string.IsNullOrWhiteSpace(strictLm.Groups["lv"].Value))
            {
                extracted[normalizedName] = strictLm.Groups["lv"].Value.Trim();
                continue;
            }

            var labelAlt = AlternateLabelMatchers(rawLabel);
            var flexLineRx = $@"(?im)^\s*(?:{labelAlt})\s*[:\u003a\u2013\-]+\s*(?<lv>[^\r\n]+)\s*$";
            var flexLm = Regex.Match(content, flexLineRx);
            if (flexLm.Success && !string.IsNullOrWhiteSpace(flexLm.Groups["lv"].Value))
            {
                extracted[normalizedName] = flexLm.Groups["lv"].Value.Trim();
                continue;
            }

            var siblingStop = BuildSiblingLabelStopAhead(rawLabel, allNamesTrimmed);
            var strictInlineRx = $@"(?is)(?:^|[\s,;])(?:{Regex.Escape(rawLabel)})\s*[:\u003a\u2013\-]+\s*(?<mv>.+?){siblingStop}";
            var si = Regex.Match(content, strictInlineRx);
            if (si.Success && !string.IsNullOrWhiteSpace(si.Groups["mv"].Value))
            {
                extracted[normalizedName] = si.Groups["mv"].Value.Trim();
                continue;
            }

            var flexInlineRx = $@"(?is)(?:^|[\s,;])(?:{labelAlt})\s*[:\u003a\u2013\-]+\s*(?<mv>.+?){siblingStop}";
            var fi = Regex.Match(content, flexInlineRx);
            if (fi.Success && !string.IsNullOrWhiteSpace(fi.Groups["mv"].Value))
            {
                extracted[normalizedName] = fi.Groups["mv"].Value.Trim();
                continue;
            }

            if (!string.IsNullOrWhiteSpace(field.ExampleValue))
            {
                var idx = content.IndexOf(field.ExampleValue, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    extracted[normalizedName] = field.ExampleValue.Trim();
                }
            }
        }

        return extracted;
    }

    private static int? GetOptionalInt(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var el))
        {
            return null;
        }

        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var i))
        {
            return i;
        }

        if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static bool? GetOptionalBool(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var el))
        {
            return null;
        }

        if (el.ValueKind == JsonValueKind.True || el.ValueKind == JsonValueKind.False)
        {
            return el.GetBoolean();
        }

        if (el.ValueKind == JsonValueKind.String && bool.TryParse(el.GetString(), out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static string? GetOptionalString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var el) || el.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString();
    }

    private static string ApplyTemplateTokens(
        string template,
        Message message,
        string classificationLabel,
        double classificationScore,
        IReadOnlyDictionary<string, string>? workflowContext = null)
    {
        var subject = message.Subject ?? "(no subject)";
        var from = message.From?.EmailAddress?.Address ?? "unknown";
        var received = message.ReceivedDateTime?.UtcDateTime.ToString("u") ?? DateTime.UtcNow.ToString("u");

        var result = template
            .Replace("{classificationLabel}", classificationLabel, StringComparison.OrdinalIgnoreCase)
            .Replace("{classificationScore}", Math.Round(classificationScore, 4).ToString("0.####", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{subject}", subject, StringComparison.OrdinalIgnoreCase)
            .Replace("{from}", from, StringComparison.OrdinalIgnoreCase)
            .Replace("{receivedDate}", received, StringComparison.OrdinalIgnoreCase);

        if (workflowContext == null || workflowContext.Count == 0)
        {
            return result;
        }

        result = Regex.Replace(result, @"\{field:([a-zA-Z0-9_\- ]+)\}", match =>
        {
            var key = NormalizeFieldToken(match.Groups[1].Value);
            return workflowContext.TryGetValue(key, out var value) ? value : string.Empty;
        });

        if (workflowContext.TryGetValue("extractionJson", out var extractionJson))
        {
            result = result.Replace("{extractionJson}", extractionJson, StringComparison.OrdinalIgnoreCase);
        }

        if (workflowContext.TryGetValue("summary", out var summary))
        {
            result = result
                .Replace("{summary}", summary, StringComparison.OrdinalIgnoreCase)
                .Replace("{summarisation}", summary, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }

    private DateTime ResolveCalendarDate(
        DateTime effectiveDate,
        int calendarDateOffsetDays,
        string? calendarDateTemplate,
        Message message,
        string classificationLabel,
        double classificationScore,
        IReadOnlyDictionary<string, string>? workflowContext)
    {
        DateTime baseDate;
        if (!string.IsNullOrWhiteSpace(calendarDateTemplate))
        {
            var rendered = ApplyTemplateTokens(
                    calendarDateTemplate,
                    message,
                    classificationLabel,
                    classificationScore,
                    workflowContext)
                .Trim();

            if (TryParseExtractedDate(rendered, out var parsed))
            {
                baseDate = parsed;
            }
            else
            {
                _logger.LogWarning(
                    "Calendar date template '{Template}' resolved to '{Rendered}' which could not be parsed; using effective date {EffectiveDate:u}.",
                    calendarDateTemplate,
                    rendered,
                    effectiveDate);
                baseDate = effectiveDate;
            }
        }
        else
        {
            baseDate = effectiveDate;
        }

        return baseDate.AddDays(calendarDateOffsetDays).Date;
    }

    private static bool TryParseExtractedDate(string value, out DateTime date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        string[] formats =
        [
            "yyyy-MM-dd",
            "dd/MM/yyyy",
            "d/M/yyyy",
            "dd-MM-yyyy",
            "d-M-yyyy",
            "dd MMM yyyy",
            "dd MMMM yyyy",
            "d MMM yyyy",
            "d MMMM yyyy",
            "yyyy/MM/dd",
            "MM/dd/yyyy",
        ];

        if (DateTime.TryParseExact(trimmed, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            return true;
        }

        if (DateTime.TryParse(trimmed, CultureInfo.GetCultureInfo("en-ZA"), DateTimeStyles.None, out date))
        {
            return true;
        }

        if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date))
        {
            return true;
        }

        return DateTime.TryParse(trimmed, out date);
    }

    private async Task<decimal?> FetchLiveZarToGbpRateAsync(CancellationToken cancellationToken)
    {
        var endpoint = _configuration["Fx:ZarBaseUrl"] ??
                       Environment.GetEnvironmentVariable("Fx__ZarBaseUrl") ??
                       "https://open.er-api.com/v6/latest/ZAR";

        try
        {
            using var response = await FxHttpClient.GetAsync(endpoint, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("FX API request failed with status {StatusCode}", response.StatusCode);
                return null;
            }

            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(payload);
            if (!doc.RootElement.TryGetProperty("rates", out var ratesEl))
            {
                _logger.LogWarning("FX API payload missing rates object.");
                return null;
            }

            if (!ratesEl.TryGetProperty("GBP", out var gbpEl))
            {
                _logger.LogWarning("FX API payload missing GBP rate.");
                return null;
            }

            decimal rate;
            if (gbpEl.ValueKind == JsonValueKind.Number)
            {
                rate = gbpEl.GetDecimal();
            }
            else if (gbpEl.ValueKind == JsonValueKind.String &&
                     decimal.TryParse(gbpEl.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            {
                rate = parsed;
            }
            else
            {
                _logger.LogWarning("FX API GBP rate could not be parsed.");
                return null;
            }

            if (rate <= 0)
            {
                _logger.LogWarning("FX API GBP rate was non-positive: {Rate}", rate);
                return null;
            }

            _logger.LogInformation("Fetched live ZAR->GBP rate: {Rate}", rate);
            return Math.Round(rate, 10);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch live ZAR->GBP rate.");
            return null;
        }
    }

    private async Task<string?> LoadSummarisationTemplatePromptAsync(int summarisationTemplateId, CancellationToken cancellationToken)
    {
        var connectionString = GetRequired("ConnectionStrings:DefaultConnection", "ConnectionStrings__DefaultConnection");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT TOP 1 summarisationPrompt
            FROM tbldocumentsummarisationtemplate
            WHERE documentSummarisationTemplateID = @templateId
              AND isActive = 1;
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@templateId", summarisationTemplateId);

        var prompt = await command.ExecuteScalarAsync(cancellationToken);
        return prompt?.ToString();
    }

    private static string BuildHeuristicSummary(string content, string prompt, int maxSentences)
    {
        var cleaned = Regex.Replace(content ?? string.Empty, @"\s+", " ").Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return string.Empty;
        }

        var sentenceMatches = Regex.Matches(cleaned, @"[^.!?\n]+[.!?]?");
        var sentences = sentenceMatches
            .Select(m => m.Value.Trim())
            .Where(s => s.Length >= 16)
            .Take(60)
            .ToList();
        if (sentences.Count == 0)
        {
            return cleaned.Length > 500 ? cleaned[..500] + "..." : cleaned;
        }

        var promptTokens = ExtractTokens(prompt);
        var ranked = sentences
            .Select((text, index) =>
            {
                var lower = text.ToLowerInvariant();
                var tokenHits = promptTokens.Count(token => lower.Contains(token, StringComparison.Ordinal));
                var score = tokenHits * 10 + Math.Min(text.Length, 220) / 40.0;
                return (text, index, score);
            })
            .OrderByDescending(x => x.score)
            .ThenBy(x => x.index)
            .Take(Math.Clamp(maxSentences, 1, 8))
            .OrderBy(x => x.index)
            .Select(x => x.text)
            .ToList();

        return string.Join(" ", ranked);
    }

    private static string NormalizeFieldToken(string value)
    {
        var normalized = Regex.Replace(value.ToLowerInvariant(), @"[^a-z0-9]+", "_").Trim('_');
        return normalized;
    }

    private static string CleanupEmailBody(string rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return string.Empty;
        }

        var text = Regex.Replace(rawBody, "<[^>]+>", " ");
        text = WebUtility.HtmlDecode(text);
        text = text.Replace('\u00A0', ' ');
        text = Regex.Replace(text, @"\s+", " ").Trim();

        var signatureMarkers = new[]
        {
            "kind regards",
            "best regards",
            "sent from my",
            "this email and any attachments",
            "confidentiality notice"
        };

        var lower = text.ToLowerInvariant();
        var cutIndex = signatureMarkers
            .Select(marker => lower.IndexOf(marker, StringComparison.Ordinal))
            .Where(index => index >= 0)
            .DefaultIfEmpty(-1)
            .Min();

        if (cutIndex > 0)
        {
            return text[..cutIndex].Trim();
        }

        return text;
    }

    private static string BuildConsolidatedContent(string subject, string cleanedBody, List<string> attachmentChunks)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Subject: {subject}");
        builder.AppendLine();
        builder.AppendLine("Body:");
        builder.AppendLine(cleanedBody);
        builder.AppendLine();
        builder.AppendLine("Attachments:");
        foreach (var chunk in attachmentChunks)
        {
            builder.AppendLine(chunk);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    /// <summary>
    /// Text passed to RunExtraction: same envelope as Document Hub workflow tests ("File name" + "Content") so templates match uploaded PDF previews.
    /// </summary>
    private static string BuildExtractionBundledAttachments(IReadOnlyList<(string FileName, string Text)> documents)
    {
        if (documents.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        for (var i = 0; i < documents.Count; i++)
        {
            var (fileName, text) = documents[i];
            sb.AppendLine($"File name: {fileName.Trim()}");
            sb.AppendLine();
            sb.AppendLine("Content:");
            sb.AppendLine((text ?? string.Empty).Trim());
            if (i < documents.Count - 1)
            {
                sb.AppendLine();
                sb.AppendLine();
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static bool IsNonInspectableAttachmentPlaceholder(string extracted)
    {
        var t = extracted.TrimStart();
        return t.StartsWith("[Image attachment detected:", StringComparison.OrdinalIgnoreCase)
            || t.StartsWith("[Attachment ", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<(List<AttachmentPreview> Attachments, List<string> ExtractedTextChunks, string ExtractionDocumentsBundled)> ExtractAttachmentContentAsync(
        GraphServiceClient graphClient,
        string mailboxUser,
        Message message,
        CancellationToken cancellationToken)
    {
        var attachmentPreviews = new List<AttachmentPreview>();
        var extractedTextChunks = new List<string>();
        var documentsForEntityExtraction = new List<(string FileName, string Text)>();
        var attachments = message.Attachments?.ToList() ?? new List<Attachment>();

        foreach (var attachment in attachments)
        {
            if (attachment is not FileAttachment fileAttachment)
            {
                continue;
            }

            var preview = new AttachmentPreview
            {
                Name = fileAttachment.Name ?? "(unnamed attachment)",
                ContentType = fileAttachment.ContentType ?? string.Empty,
                ExtractionStatus = "not_processed"
            };

            byte[]? contentBytes = fileAttachment.ContentBytes;
            if (contentBytes == null && message.Id != null && attachment.Id != null)
            {
                var expanded = await graphClient.Users[mailboxUser]
                    .Messages[message.Id]
                    .Attachments[attachment.Id]
                    .GetAsync(cancellationToken: cancellationToken);

                if (expanded is FileAttachment expandedFile)
                {
                    contentBytes = expandedFile.ContentBytes;
                }
            }

            if (contentBytes == null || contentBytes.Length == 0)
            {
                preview.ExtractionStatus = "empty_or_unavailable";
                attachmentPreviews.Add(preview);
                continue;
            }

            preview.ContentBytes = contentBytes;

            try
            {
                var extracted = ExtractTextFromAttachment(preview.Name, preview.ContentType, contentBytes);
                if (!string.IsNullOrWhiteSpace(extracted))
                {
                    extractedTextChunks.Add($"Attachment {preview.Name}: {extracted}");
                    preview.ExtractionStatus = "extracted";
                    if (!IsNonInspectableAttachmentPlaceholder(extracted))
                    {
                        documentsForEntityExtraction.Add((preview.Name, extracted));
                    }
                }
                else
                {
                    preview.ExtractionStatus = "no_text_extracted";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Attachment extraction failed for {AttachmentName}", preview.Name);
                preview.ExtractionStatus = "extraction_failed";
            }

            attachmentPreviews.Add(preview);
        }

        var extractionBundled = BuildExtractionBundledAttachments(documentsForEntityExtraction);
        return (attachmentPreviews, extractedTextChunks, extractionBundled);
    }

    private static string ExtractTextFromAttachment(string fileName, string contentType, byte[] content)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".pdf" => ExtractPdfText(content),
            ".docx" => ExtractDocxText(content),
            ".xlsx" => ExtractXlsxText(content),
            ".eml" => ExtractEmlText(content),
            ".msg" => ExtractMsgText(content),
            ".txt" or ".csv" or ".json" or ".xml" => Encoding.UTF8.GetString(content),
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".webp" => $"[Image attachment detected: {fileName} ({contentType})]",
            _ => $"[Attachment {fileName} ({contentType}) is not currently text-extracted.]"
        };
    }

    private static string ExtractPdfText(byte[] content)
    {
        using var stream = new MemoryStream(content);
        using var pdf = PdfDocument.Open(stream);
        var builder = new StringBuilder();
        foreach (var page in pdf.GetPages())
        {
            builder.AppendLine(page.Text);
        }

        return NormalizeWhitespacePreserveLineBreaks(builder.ToString());
    }

    private static string ExtractDocxText(byte[] content)
    {
        using var stream = new MemoryStream(content);
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart?.Document?.Body?.InnerText ?? string.Empty;
        return NormalizeWhitespace(body);
    }

    private static string ExtractXlsxText(byte[] content)
    {
        try
        {
            using var stream = new MemoryStream(content);
            using var document = SpreadsheetDocument.Open(stream, false);
            var workbookPart = document.WorkbookPart;
            if (workbookPart?.Workbook?.Sheets == null)
            {
                return string.Empty;
            }

            var sharedStrings = workbookPart.SharedStringTablePart?.SharedStringTable;
            var sb = new StringBuilder();
            var sheetCount = 0;

            foreach (var sheet in workbookPart.Workbook.Sheets.Elements<Sheet>())
            {
                if (sheetCount++ >= 20)
                {
                    break;
                }

                if (sheet.Id?.Value == null)
                {
                    continue;
                }

                if (workbookPart.GetPartById(sheet.Id.Value) is not WorksheetPart worksheetPart)
                {
                    continue;
                }

                var sheetData = worksheetPart.Worksheet?.GetFirstChild<SheetData>();
                if (sheetData == null)
                {
                    continue;
                }

                sb.AppendLine($"Sheet: {sheet.Name?.Value ?? "Sheet"}");

                var rowCount = 0;
                foreach (var row in sheetData.Elements<Row>())
                {
                    if (rowCount++ >= 2000)
                    {
                        break;
                    }

                    var line = FormatSpreadsheetRow(row, sharedStrings);
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        sb.AppendLine(line);
                    }
                }

                sb.AppendLine();
            }

            return NormalizeWhitespacePreserveLineBreaks(sb.ToString());
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string FormatSpreadsheetRow(Row row, SharedStringTable? sharedStrings)
    {
        var cells = row.Elements<Cell>()
            .Select(cell => (ColumnIndex: GetSpreadsheetColumnIndex(cell.CellReference?.Value), Text: GetSpreadsheetCellText(cell, sharedStrings)))
            .Where(cell => cell.ColumnIndex > 0)
            .OrderBy(cell => cell.ColumnIndex)
            .ToList();

        if (cells.Count == 0)
        {
            return string.Empty;
        }

        var values = new string[cells[^1].ColumnIndex];
        foreach (var (columnIndex, text) in cells)
        {
            values[columnIndex - 1] = text;
        }

        while (values.Length > 0 && string.IsNullOrWhiteSpace(values[^1]))
        {
            Array.Resize(ref values, values.Length - 1);
        }

        return values.Length == 0 ? string.Empty : string.Join(" | ", values);
    }

    private static string GetSpreadsheetCellText(Cell cell, SharedStringTable? sharedStrings)
    {
        if (cell.InlineString?.Text != null)
        {
            return cell.InlineString.Text.Text?.Trim() ?? string.Empty;
        }

        if (cell.CellValue == null)
        {
            return string.Empty;
        }

        var raw = cell.CellValue.InnerText?.Trim() ?? string.Empty;
        if (cell.DataType?.Value == CellValues.SharedString &&
            sharedStrings != null &&
            int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sharedIndex))
        {
            return sharedStrings.ElementAtOrDefault(sharedIndex)?.InnerText?.Trim() ?? raw;
        }

        return raw;
    }

    private static int GetSpreadsheetColumnIndex(string? cellReference)
    {
        if (string.IsNullOrWhiteSpace(cellReference))
        {
            return 0;
        }

        var columnLetters = new string(cellReference.TakeWhile(char.IsLetter).ToArray());
        if (columnLetters.Length == 0)
        {
            return 0;
        }

        var index = 0;
        foreach (var letter in columnLetters)
        {
            index = (index * 26) + (char.ToUpperInvariant(letter) - 'A' + 1);
        }

        return index;
    }

    private static string ExtractEmlText(byte[] content)
    {
        using var stream = new MemoryStream(content);
        var message = MimeMessage.Load(stream);
        var body = message.TextBody ?? message.HtmlBody ?? string.Empty;
        return NormalizeWhitespace(CleanupEmailBody(body));
    }

    private static string ExtractMsgText(byte[] content)
    {
        using var stream = new MemoryStream(content);
        using var message = new MsgReader.Outlook.Storage.Message(stream);
        var builder = new StringBuilder();
        builder.AppendLine(message.Subject);
        builder.AppendLine(message.BodyText);
        return NormalizeWhitespace(builder.ToString());
    }

    private static string NormalizeWhitespace(string value)
    {
        return Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim();
    }

    private static string NormalizeWhitespacePreserveLineBreaks(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Replace("\r\n", "\n").Replace('\r', '\n');
        var sb = new StringBuilder(capacity: Math.Min(normalized.Length, 512 * 1024));
        foreach (var line in normalized.Split('\n'))
        {
            var collapsed = Regex.Replace(line, @"[ \t]+", " ").Trim();
            if (collapsed.Length > 0)
            {
                sb.AppendLine(collapsed);
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static Dictionary<string, string>? CloneWorkflowContextForPreview(Dictionary<string, string> source)
    {
        if (source.Count == 0)
        {
            return null;
        }

        var preview = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in source)
        {
            var limit = kv.Key.Equals("extractionJson", StringComparison.OrdinalIgnoreCase) ? 12000 :
                kv.Key.Equals("summary", StringComparison.OrdinalIgnoreCase) ||
                kv.Key.Equals("summarisation", StringComparison.OrdinalIgnoreCase) ? 8000 :
                4000;
            var v = kv.Value ?? string.Empty;
            preview[kv.Key] = v.Length <= limit ? v : $"{v[..limit]} …(truncated)";
        }

        return preview;
    }

    private static (string? SummarisationText, string? ExtractionJson) ExtractAuditOutputsFromWorkflowContext(
        IReadOnlyDictionary<string, string> workflowContext)
    {
        if (workflowContext.Count == 0)
        {
            return (null, null);
        }

        string? summarisation = null;
        if (workflowContext.TryGetValue("summary", out var summary) && !string.IsNullOrWhiteSpace(summary))
        {
            summarisation = summary.Trim();
        }
        else if (workflowContext.TryGetValue("summarisation", out var summarisationAlt) &&
                 !string.IsNullOrWhiteSpace(summarisationAlt))
        {
            summarisation = summarisationAlt.Trim();
        }

        string? extractionJson = null;
        if (workflowContext.TryGetValue("extractionJson", out var extraction) && !string.IsNullOrWhiteSpace(extraction))
        {
            extractionJson = extraction.Trim();
        }

        return (summarisation, extractionJson);
    }

    private string GetRequired(string configKey, string envKey)
    {
        var value = _configuration[configKey] ?? Environment.GetEnvironmentVariable(envKey);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Required configuration is missing: {envKey}");
        }

        return value;
    }
}

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Azure.Identity;
using DocumentFormat.OpenXml.Packaging;
using email_processor_function.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.Messages.Item.Move;
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
    private readonly ILogger<GraphEmailReader> _logger;

    public GraphEmailReader(IConfiguration configuration, ILogger<GraphEmailReader> logger)
    {
        _configuration = configuration;
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
                cfg.QueryParameters.Select = new[] { "id", "subject", "from", "receivedDateTime", "hasAttachments", "categories" };
                cfg.QueryParameters.Filter = $"not(categories/any(c:c eq '{CompletedCategory}'))";
            }, cancellationToken);

        var allFetched = messagesPage?.Value ?? new List<Message>();
        // Extra in-code guard in case mailbox categories are case-variant or filter behavior differs.
        var eligibleMessages = allFetched
            .Where(x => !(x.Categories?.Any(c => c.Equals(CompletedCategory, StringComparison.OrdinalIgnoreCase)) ?? false))
            .ToList();

        var processedPreviews = new List<EmailMessagePreview>();
        var unclassifiedCount = 0;
        foreach (var message in eligibleMessages)
        {
            var detailedMessage = await graphClient.Users[mailboxUser]
                .Messages[message.Id!]
                .GetAsync(cfg =>
                {
                    cfg.QueryParameters.Select = new[] { "id", "subject", "from", "receivedDateTime", "hasAttachments", "body", "categories" };
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

            var classification = ClassifyAgainstTemplates(consolidatedContent, templates);
            if (classification.Label.Equals("Unclassified", StringComparison.OrdinalIgnoreCase))
            {
                unclassifiedCount++;
            }

            var postActions = await ExecuteWorkflowAsync(
                graphClient,
                mailboxUser,
                propertyHubFolder.Id,
                detailedMessage,
                attachmentExtraction.Attachments,
                consolidatedContent,
                classification.Label,
                classification.Score,
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
                Attachments = attachmentExtraction.Attachments
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

    private async Task<string> ExecuteWorkflowAsync(
        GraphServiceClient graphClient,
        string mailboxUser,
        string propertyHubFolderId,
        Message message,
        List<AttachmentPreview> attachmentPreviews,
        string consolidatedContent,
        string classificationLabel,
        double classificationScore,
        List<WorkflowRule> rules,
        CancellationToken cancellationToken)
    {
        if (message.Id == null)
        {
            return "processed";
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
                        cancellationToken);
                }
                return "processed";
            }

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
                    await ApplyCategoryAsync(graphClient, mailboxUser, currentMessageId, null, CompletedCategory, null, cancellationToken);
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
                        await RunExtractionStepAsync(
                            consolidatedContent,
                            step.StepConfigJson,
                            workflowContext,
                            cancellationToken);
                    }
                    else if (stepType == "runsummarisation")
                    {
                        await RunSummarisationStepAsync(
                            consolidatedContent,
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
                            await FinalizeWorkflowAuditRunAsync(
                                auditConnection,
                                auditRunId.Value,
                                "Failed",
                                $"{step.StepType}: {ex.Message}",
                                cancellationToken);
                        }
                        return $"workflow_failed:{step.StepType}";
                    }
                }
            }

            if (auditConnection != null && auditRunId.HasValue)
            {
                await FinalizeWorkflowAuditRunAsync(
                    auditConnection,
                    auditRunId.Value,
                    "Completed",
                    null,
                    cancellationToken);
            }

            return "workflow_applied";
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
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE tbldocumentworkflowauditrun
            SET Status = @status,
                ErrorMessage = @errorMessage,
                CompletedDate = SYSUTCDATETIME()
            WHERE DocumentWorkflowAuditRunId = @auditRunId;
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@auditRunId", auditRunId);
        command.Parameters.AddWithValue("@status", status);
        command.Parameters.AddWithValue("@errorMessage", (object?)errorMessage ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
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

        if (!string.IsNullOrWhiteSpace(stepConfigJson))
        {
            using var config = JsonDocument.Parse(stepConfigJson);
            var root = config.RootElement;
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
        }

        effectiveDate = effectiveDate.AddDays(transactionDateOffsetDays);
        var renderedDescription = ApplyTemplateTokens(
            descriptionTemplate ??
            "Workflow auto-created journal log for {classificationLabel} (score {classificationScore}) from email '{subject}'.",
            message,
            classificationLabel,
            classificationScore,
            workflowContext);
        var renderedReference = ApplyTemplateTokens(journalReferenceTemplate ?? string.Empty, message, classificationLabel, classificationScore, workflowContext);
        var renderedAmountRand = ApplyTemplateTokens(amountRandTemplate ?? string.Empty, message, classificationLabel, classificationScore, workflowContext);
        var renderedAmountGbp = ApplyTemplateTokens(amountGbpTemplate ?? string.Empty, message, classificationLabel, classificationScore, workflowContext);
        var renderedExchange = ApplyTemplateTokens(exchangeRateTemplate ?? string.Empty, message, classificationLabel, classificationScore, workflowContext);
        var amountRand = ParseDecimalOrNull(renderedAmountRand);
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
                journalReference)
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
                @journalReference);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;
        await using var command = new SqlCommand(insertSql, connection);
        var propertyGroupIdForInsert = propertyId.HasValue
            ? await ResolvePropertyGroupIdAsync(connection, propertyId.Value, cancellationToken)
            : null;
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
        command.Parameters.AddWithValue("@contactNotes", ApplyTemplateTokens(
            notesTemplate ??
            "Workflow auto-created contact log for {classificationLabel} (score {classificationScore}) from '{from}' re '{subject}'.",
            message,
            classificationLabel,
            classificationScore,
            workflowContext));
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
        _logger.LogInformation("Created ContactLog {ContactLogId} via workflow.", insertedId);
    }

    private async Task RunExtractionStepAsync(
        string consolidatedContent,
        string? stepConfigJson,
        Dictionary<string, string> workflowContext,
        CancellationToken cancellationToken)
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

        var extracted = ExtractFieldsFromContent(consolidatedContent, fields);
        foreach (var item in extracted)
        {
            workflowContext[item.Key] = item.Value;
        }

        workflowContext["extractionJson"] = JsonSerializer.Serialize(extracted);
        _logger.LogInformation(
            "RunExtraction template {TemplateId} extracted {Count} field(s).",
            extractionTemplateId.Value,
            extracted.Count);
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

        var summary = BuildHeuristicSummary(consolidatedContent, prompt, maxSentences);
        workflowContext["summary"] = summary;
        workflowContext["summarisation"] = summary;
        workflowContext["summaryPrompt"] = prompt.Trim();
        _logger.LogInformation("RunSummarisation generated summary length {Length}.", summary.Length);
    }

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "for", "with", "from", "this", "that", "are", "was", "were", "into", "onto", "your", "their",
        "have", "has", "had", "will", "shall", "would", "could", "should", "about", "over", "under", "between", "per",
        "each", "any", "all", "not", "but", "can", "may", "might", "than", "then", "also", "such", "via"
    };

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

    private async Task EnsureMasterCategoryAsync(
        GraphServiceClient graphClient,
        string mailboxUser,
        string categoryName,
        string? categoryColor,
        CancellationToken cancellationToken)
    {
        OutlookCategory? existingCategory = null;
        var escaped = categoryName.Replace("'", "''");
        var existing = await graphClient.Users[mailboxUser]
            .Outlook
            .MasterCategories
            .GetAsync(cfg =>
            {
                cfg.QueryParameters.Filter = $"displayName eq '{escaped}'";
                cfg.QueryParameters.Top = 1;
            }, cancellationToken);
        existingCategory = existing?.Value?.FirstOrDefault();

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

        return TryEvaluateDecimalExpression(value, out var parsed) ? parsed : null;
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
            .Replace(",", string.Empty, StringComparison.Ordinal)
            .Replace("R", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("ZAR", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("GBP", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("£", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);

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

    private static async Task CreateJournalAttachmentRowsAsync(
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
            await using var command = new SqlCommand(insertSql, connection);
            command.Parameters.AddWithValue("@journalLogId", journalLogId);
            command.Parameters.AddWithValue("@dateAttached", DateTime.UtcNow);

            var attachedByValue = NullIfEmpty(attachedBy);
            if (!string.IsNullOrWhiteSpace(attachment.Name))
            {
                attachedByValue = string.IsNullOrWhiteSpace(attachedByValue)
                    ? $"Workflow ({attachment.Name})"
                    : $"{attachedByValue} ({attachment.Name})";
            }

            var safeAttachedBy = attachedByValue;
            if (!string.IsNullOrWhiteSpace(safeAttachedBy) && safeAttachedBy.Length > 255)
            {
                safeAttachedBy = safeAttachedBy[..255];
            }
            command.Parameters.AddWithValue("@attachedBy", (object?)safeAttachedBy ?? DBNull.Value);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task CreateContactAttachmentRowsAsync(
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
            await using var command = new SqlCommand(insertSql, connection);
            command.Parameters.AddWithValue("@contactId", (object?)NullIfEmpty(contactId) ?? DBNull.Value);
            command.Parameters.AddWithValue("@contactLogId", contactLogId);

            var description = ApplyTemplateTokens(
                attachmentDescriptionTemplate ?? $"Attachment: {attachment.Name}",
                message,
                classificationLabel,
                classificationScore,
                workflowContext);
            command.Parameters.AddWithValue("@attachmentDescription", (object?)NullIfEmpty(description) ?? DBNull.Value);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static Dictionary<string, string> ExtractFieldsFromContent(
        string content,
        IEnumerable<(string FieldName, string? ExampleValue)> fields)
    {
        var extracted = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in fields)
        {
            var normalizedName = NormalizeFieldToken(field.FieldName);
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                continue;
            }

            var pattern = $@"(?im)^\s*{Regex.Escape(field.FieldName)}\s*[:\-]\s*(.+)$";
            var match = Regex.Match(content, pattern);
            if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
            {
                extracted[normalizedName] = match.Groups[1].Value.Trim();
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

    private async Task<(List<AttachmentPreview> Attachments, List<string> ExtractedTextChunks)> ExtractAttachmentContentAsync(
        GraphServiceClient graphClient,
        string mailboxUser,
        Message message,
        CancellationToken cancellationToken)
    {
        var attachmentPreviews = new List<AttachmentPreview>();
        var extractedTextChunks = new List<string>();
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

            try
            {
                var extracted = ExtractTextFromAttachment(preview.Name, preview.ContentType, contentBytes);
                if (!string.IsNullOrWhiteSpace(extracted))
                {
                    extractedTextChunks.Add($"Attachment {preview.Name}: {extracted}");
                    preview.ExtractionStatus = "extracted";
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

        return (attachmentPreviews, extractedTextChunks);
    }

    private static string ExtractTextFromAttachment(string fileName, string contentType, byte[] content)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".pdf" => ExtractPdfText(content),
            ".docx" => ExtractDocxText(content),
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
            builder.Append(' ');
            builder.Append(page.Text);
        }

        return NormalizeWhitespace(builder.ToString());
    }

    private static string ExtractDocxText(byte[] content)
    {
        using var stream = new MemoryStream(content);
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart?.Document?.Body?.InnerText ?? string.Empty;
        return NormalizeWhitespace(body);
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

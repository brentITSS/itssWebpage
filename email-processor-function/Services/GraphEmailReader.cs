using System.Text;
using System.Text.RegularExpressions;
using Azure.Identity;
using DocumentFormat.OpenXml.Packaging;
using email_processor_function.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
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

            processedPreviews.Add(new EmailMessagePreview
            {
                MessageId = detailedMessage.Id ?? string.Empty,
                Subject = detailedMessage.Subject ?? "(no subject)",
                From = detailedMessage.From?.EmailAddress?.Address,
                ReceivedDateTime = detailedMessage.ReceivedDateTime,
                HasAttachments = detailedMessage.HasAttachments ?? false,
                Categories = detailedMessage.Categories?.ToList() ?? new List<string>(),
                ProcessingStatus = "processed",
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
                bestScore = score;
                bestCoreScore = coreScore;
                bestLabelCoverage = labelCoverage;
                bestPhraseBoost = phraseBoost;
                bestTemplate = template;
                bestOverlapTerms = overlapTerms;
            }
        }

        // Conservative threshold to avoid false matches.
        if (bestTemplate == null || bestScore < 0.28)
        {
            var explanation = bestTemplate == null
                ? "No template produced a meaningful lexical overlap with the email content."
                : $"Best template confidence {Math.Round(bestScore, 4)} is below threshold 0.28. " +
                  $"Closest template '{bestTemplate.ClassificationLabel}' had core={Math.Round(bestCoreScore, 4)}, " +
                  $"labelCoverage={Math.Round(bestLabelCoverage, 4)}, phraseBoost={Math.Round(bestPhraseBoost, 2)} " +
                  $"with overlap terms: {string.Join(", ", bestOverlapTerms.DefaultIfEmpty("none"))}.";
            return ("Unclassified", Math.Round(bestScore, 4), explanation);
        }

        var explainability =
            $"Matched template '{bestTemplate.ClassificationLabel}' with confidence {Math.Round(bestScore, 4)} " +
            $"(core={Math.Round(bestCoreScore, 4)}, labelCoverage={Math.Round(bestLabelCoverage, 4)}, phraseBoost={Math.Round(bestPhraseBoost, 2)}). " +
            $"Overlap terms: {string.Join(", ", bestOverlapTerms.DefaultIfEmpty("none"))}.";

        return (bestTemplate.ClassificationLabel, Math.Round(bestScore, 4), explainability);
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

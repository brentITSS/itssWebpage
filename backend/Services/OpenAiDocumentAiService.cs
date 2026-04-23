using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using backend.DTOs;

namespace backend.Services;

public class OpenAiDocumentAiService : IDocumentAiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAiDocumentAiService> _logger;

    public OpenAiDocumentAiService(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAiDocumentAiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> SummariseAsync(string extractedText, string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(extractedText))
        {
            return DocumentHubAiHelper.BuildSummary(extractedText, prompt);
        }

        var apiKey = _configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OpenAI__ApiKey");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return DocumentHubAiHelper.BuildSummary(extractedText, prompt);
        }

        var model = _configuration["OpenAI:Model"] ?? Environment.GetEnvironmentVariable("OpenAI__Model") ?? "gpt-4o-mini";
        var systemPrompt =
            "You are a document summarisation assistant. " +
            "Follow the user prompt strictly, especially word limits and tone. " +
            "Return only the summary text with no preamble.";

        var userPrompt = $"User summarisation prompt:\n{prompt}\n\nDocument text:\n{TrimForModel(extractedText, 14000)}";

        try
        {
            var content = await CreateChatCompletionAsync(apiKey, model, systemPrompt, userPrompt, 320, cancellationToken);
            if (string.IsNullOrWhiteSpace(content))
            {
                return DocumentHubAiHelper.BuildSummary(extractedText, prompt);
            }

            return content.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI summarisation failed, using heuristic fallback.");
            return DocumentHubAiHelper.BuildSummary(extractedText, prompt);
        }
    }

    public async Task<DocumentClassificationSuggestionDto> BuildClassificationSuggestionAsync(
        string fileName,
        string extractedText,
        CancellationToken cancellationToken = default)
    {
        var fallbackLabel = DocumentHubAiHelper.BuildTwoWordLabel(fileName, extractedText);
        var fallbackDescription = DocumentHubAiHelper.BuildDescription(fallbackLabel, extractedText);
        var fallbackPrompt = DocumentHubAiHelper.BuildClassificationPrompt(fallbackLabel, fallbackDescription);

        var fallback = new DocumentClassificationSuggestionDto
        {
            FileName = fileName,
            SuggestedLabel = fallbackLabel,
            SuggestedDescription = fallbackDescription,
            SuggestedPrompt = fallbackPrompt,
            TextPreview = extractedText.Length > 400 ? extractedText[..400] + "..." : extractedText
        };

        var apiKey = _configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OpenAI__ApiKey");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return fallback;
        }

        var model = _configuration["OpenAI:Model"] ?? Environment.GetEnvironmentVariable("OpenAI__Model") ?? "gpt-4o-mini";
        var systemPrompt =
            "You classify business documents. Return strict JSON with keys: label, description, prompt. " +
            "label must be max two words.";
        var userPrompt =
            "Analyze the document and propose a classification seed for future matching.\n" +
            "Rules:\n" +
            "- label maximum two words\n" +
            "- description one short sentence\n" +
            "- prompt should help classify similar future docs\n" +
            $"File name: {fileName}\n" +
            $"Document text:\n{TrimForModel(extractedText, 12000)}";

        try
        {
            var content = await CreateChatCompletionAsync(apiKey, model, systemPrompt, userPrompt, 300, cancellationToken, expectJson: true);
            if (string.IsNullOrWhiteSpace(content))
            {
                return fallback;
            }

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            var label = root.TryGetProperty("label", out var labelEl) ? labelEl.GetString() : null;
            var description = root.TryGetProperty("description", out var descEl) ? descEl.GetString() : null;
            var prompt = root.TryGetProperty("prompt", out var promptEl) ? promptEl.GetString() : null;

            return new DocumentClassificationSuggestionDto
            {
                FileName = fileName,
                SuggestedLabel = string.IsNullOrWhiteSpace(label) ? fallback.SuggestedLabel : label.Trim(),
                SuggestedDescription = string.IsNullOrWhiteSpace(description) ? fallback.SuggestedDescription : description.Trim(),
                SuggestedPrompt = string.IsNullOrWhiteSpace(prompt) ? fallback.SuggestedPrompt : prompt.Trim(),
                TextPreview = fallback.TextPreview
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI classification suggestion failed, using heuristic fallback.");
            return fallback;
        }
    }

    public async Task<List<DocumentExtractionSuggestedFieldDto>> SuggestExtractionFieldsAsync(
        string extractedText,
        CancellationToken cancellationToken = default)
    {
        var fallback = BuildHeuristicExtractionSuggestions(extractedText);
        var apiKey = _configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OpenAI__ApiKey");
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(extractedText))
        {
            return fallback;
        }

        var model = _configuration["OpenAI:Model"] ?? Environment.GetEnvironmentVariable("OpenAI__Model") ?? "gpt-4o-mini";
        var systemPrompt =
            "You extract likely structured fields from business documents. " +
            "Return strict JSON: {\"fields\":[{\"fieldName\":\"...\",\"exampleValue\":\"...\"}]}. " +
            "fieldName should be concise, lowercase, and human-readable.";
        var userPrompt =
            "Suggest up to 8 likely extraction fields from this document text.\n" +
            "Only include fields with clear example values in the source.\n" +
            $"Document text:\n{TrimForModel(extractedText, 12000)}";

        try
        {
            var content = await CreateChatCompletionAsync(apiKey, model, systemPrompt, userPrompt, 500, cancellationToken, expectJson: true);
            if (string.IsNullOrWhiteSpace(content))
            {
                return fallback;
            }

            using var doc = JsonDocument.Parse(content);
            if (!doc.RootElement.TryGetProperty("fields", out var fieldsEl) || fieldsEl.ValueKind != JsonValueKind.Array)
            {
                return fallback;
            }

            var fields = new List<DocumentExtractionSuggestedFieldDto>();
            foreach (var item in fieldsEl.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var name = item.TryGetProperty("fieldName", out var nameEl) ? nameEl.GetString() : null;
                var value = item.TryGetProperty("exampleValue", out var valueEl) ? valueEl.GetString() : null;
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                fields.Add(new DocumentExtractionSuggestedFieldDto
                {
                    FieldName = name.Trim(),
                    ExampleValue = value.Trim()
                });
            }

            return fields.Count == 0 ? fallback : fields.Take(8).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI extraction field suggestion failed, using heuristic fallback.");
            return fallback;
        }
    }

    public async Task<List<DocumentExtractionSuggestedFieldDto>> SuggestFieldsFromSelectionAsync(
        string selectedText,
        string extractedText,
        CancellationToken cancellationToken = default)
    {
        var fallback = BuildSelectionFallbackSuggestions(selectedText);
        if (string.IsNullOrWhiteSpace(selectedText))
        {
            return fallback;
        }

        var apiKey = _configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OpenAI__ApiKey");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return fallback;
        }

        var model = _configuration["OpenAI:Model"] ?? Environment.GetEnvironmentVariable("OpenAI__Model") ?? "gpt-4o-mini";
        var systemPrompt =
            "You are an extraction-trainer assistant. " +
            "Infer one or more field/value pairs from a selected phrase in a PDF. " +
            "Return strict JSON: {\"fields\":[{\"fieldName\":\"...\",\"exampleValue\":\"...\"}]}.";
        var userPrompt =
            "Given selected text and document context, infer the best extraction field names and values.\n" +
            "Rules:\n" +
            "- use practical field names in snake_case\n" +
            "- if a date range is selected, return start and end fields when obvious\n" +
            "- only return values present in selected text\n\n" +
            $"Selected text:\n{selectedText}\n\n" +
            $"Document context:\n{TrimForModel(extractedText, 6000)}";

        try
        {
            var content = await CreateChatCompletionAsync(apiKey, model, systemPrompt, userPrompt, 320, cancellationToken, expectJson: true);
            if (string.IsNullOrWhiteSpace(content))
            {
                return fallback;
            }

            using var doc = JsonDocument.Parse(content);
            if (!doc.RootElement.TryGetProperty("fields", out var fieldsEl) || fieldsEl.ValueKind != JsonValueKind.Array)
            {
                return fallback;
            }

            var fields = new List<DocumentExtractionSuggestedFieldDto>();
            foreach (var item in fieldsEl.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var name = item.TryGetProperty("fieldName", out var nameEl) ? nameEl.GetString() : null;
                var value = item.TryGetProperty("exampleValue", out var valueEl) ? valueEl.GetString() : null;
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                fields.Add(new DocumentExtractionSuggestedFieldDto
                {
                    FieldName = name.Trim(),
                    ExampleValue = value.Trim()
                });
            }

            return fields.Count == 0 ? fallback : fields.Take(4).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI selected-text extraction failed, using fallback.");
            return fallback;
        }
    }

    private async Task<string?> CreateChatCompletionAsync(
        string apiKey,
        string model,
        string systemPrompt,
        string userPrompt,
        int maxTokens,
        CancellationToken cancellationToken,
        bool expectJson = false)
    {
        var baseUrl = _configuration["OpenAI:BaseUrl"] ?? Environment.GetEnvironmentVariable("OpenAI__BaseUrl") ?? "https://api.openai.com";
        var endpoint = $"{baseUrl.TrimEnd('/')}/v1/chat/completions";

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new
        {
            model,
            temperature = 0.2,
            max_tokens = maxTokens,
            response_format = expectJson ? new { type = "json_object" } : null,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"OpenAI request failed ({(int)response.StatusCode}): {error}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = doc.RootElement;
        var choices = root.GetProperty("choices");
        if (choices.GetArrayLength() == 0)
        {
            return null;
        }

        var message = choices[0].GetProperty("message");
        return message.TryGetProperty("content", out var contentEl) ? contentEl.GetString() : null;
    }

    private static string TrimForModel(string text, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return text.Length <= maxChars ? text : text[..maxChars];
    }

    private static List<DocumentExtractionSuggestedFieldDto> BuildHeuristicExtractionSuggestions(string extractedText)
    {
        if (string.IsNullOrWhiteSpace(extractedText))
        {
            return new List<DocumentExtractionSuggestedFieldDto>();
        }

        var suggestions = new List<DocumentExtractionSuggestedFieldDto>();
        var matches = System.Text.RegularExpressions.Regex.Matches(
            extractedText,
            @"(?im)^\s*([A-Za-z][A-Za-z0-9 \/\-\(\)]{2,40})\s*[:\-]\s*(.{1,120})$");

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var rawName = match.Groups[1].Value.Trim();
            var rawValue = match.Groups[2].Value.Trim();
            if (rawValue.Length < 2)
            {
                continue;
            }

            var fieldName = rawName
                .ToLowerInvariant()
                .Replace("/", " ")
                .Replace("-", " ");

            suggestions.Add(new DocumentExtractionSuggestedFieldDto
            {
                FieldName = fieldName,
                ExampleValue = rawValue
            });
        }

        return suggestions
            .GroupBy(x => $"{x.FieldName}|{x.ExampleValue}", StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Take(8)
            .ToList();
    }

    private static List<DocumentExtractionSuggestedFieldDto> BuildSelectionFallbackSuggestions(string selectedText)
    {
        if (string.IsNullOrWhiteSpace(selectedText))
        {
            return new List<DocumentExtractionSuggestedFieldDto>();
        }

        var normalized = selectedText.Trim();
        var suggestions = new List<DocumentExtractionSuggestedFieldDto>();

        var dateMatches = System.Text.RegularExpressions.Regex.Matches(
            normalized,
            @"\b\d{1,2}[\/\-]\d{1,2}[\/\-]\d{2,4}\b");

        if (dateMatches.Count >= 2)
        {
            suggestions.Add(new DocumentExtractionSuggestedFieldDto
            {
                FieldName = "range_start_date",
                ExampleValue = dateMatches[0].Value
            });
            suggestions.Add(new DocumentExtractionSuggestedFieldDto
            {
                FieldName = "range_end_date",
                ExampleValue = dateMatches[1].Value
            });
        }

        if (normalized.Contains(" to ", StringComparison.OrdinalIgnoreCase) && dateMatches.Count >= 2)
        {
            suggestions.Add(new DocumentExtractionSuggestedFieldDto
            {
                FieldName = "range_date",
                ExampleValue = $"{dateMatches[0].Value} to {dateMatches[1].Value}"
            });
        }

        var keyValueMatch = System.Text.RegularExpressions.Regex.Match(normalized, @"^\s*(.+?)\s*[:\-]\s*(.+)\s*$");
        if (keyValueMatch.Success)
        {
            var key = keyValueMatch.Groups[1].Value
                .Trim()
                .ToLowerInvariant()
                .Replace(" ", "_");
            var value = keyValueMatch.Groups[2].Value.Trim();

            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
            {
                suggestions.Add(new DocumentExtractionSuggestedFieldDto
                {
                    FieldName = key,
                    ExampleValue = value
                });
            }
        }

        if (suggestions.Count == 0)
        {
            var words = normalized
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(4)
                .Select(x => x.ToLowerInvariant());
            suggestions.Add(new DocumentExtractionSuggestedFieldDto
            {
                FieldName = string.Join("_", words),
                ExampleValue = normalized
            });
        }

        return suggestions
            .GroupBy(x => $"{x.FieldName}|{x.ExampleValue}", StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Take(4)
            .ToList();
    }
}

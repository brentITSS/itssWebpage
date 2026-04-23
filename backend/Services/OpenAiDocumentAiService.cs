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
}

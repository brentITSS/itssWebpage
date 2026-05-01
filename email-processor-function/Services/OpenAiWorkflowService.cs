using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace email_processor_function.Services;

public sealed class OpenAiWorkflowService : IOpenAiWorkflowService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAiWorkflowService> _logger;

    public OpenAiWorkflowService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OpenAiWorkflowService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _httpClient.Timeout = TimeSpan.FromSeconds(120);
    }

    private string? ApiKey =>
        _configuration["OpenAI:ApiKey"] ??
        Environment.GetEnvironmentVariable("OpenAI__ApiKey");

    private string Model =>
        _configuration["OpenAI:Model"] ??
        Environment.GetEnvironmentVariable("OpenAI__Model") ??
        "gpt-4o-mini";

    private string BaseUrl =>
        (_configuration["OpenAI:BaseUrl"] ??
         Environment.GetEnvironmentVariable("OpenAI__BaseUrl") ??
         "https://api.openai.com").TrimEnd('/');

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

    public async Task<string?> SummariseDocumentAsync(
        string documentText,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(documentText))
        {
            return null;
        }

        var trimmedText = TrimForModel(documentText, 14_000);
        var systemPrompt =
            "You are a document summarisation assistant. " +
            "Follow the user prompt strictly, especially word limits and tone. " +
            "Return only the summary text with no preamble.";
        var userPayload = $"User summarisation prompt:\n{userPrompt}\n\nDocument text:\n{trimmedText}";

        try
        {
            var content = await CreateChatCompletionAsync(
                ApiKey!,
                Model,
                systemPrompt,
                userPayload,
                maxTokens: 320,
                expectJsonObject: false,
                cancellationToken).ConfigureAwait(false);

            var result = content?.Trim();
            return string.IsNullOrWhiteSpace(result) ? null : result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI workflow summarisation failed.");
            return null;
        }
    }

    public async Task<Dictionary<string, string>> ExtractWithTemplateFieldsAsync(
        string documentText,
        IReadOnlyList<(string FieldName, string? ExampleValue)> fields,
        IReadOnlyCollection<string> allowedNormalizedKeys,
        CancellationToken cancellationToken = default)
    {
        var output = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!IsConfigured || string.IsNullOrWhiteSpace(documentText) || fields.Count == 0)
        {
            return output;
        }

        var allowed = new HashSet<string>(allowedNormalizedKeys, StringComparer.OrdinalIgnoreCase);

        var fieldDescriptors = fields
            .Select(f => new
            {
                rawName = f.FieldName.Trim(),
                normalizedKey = NormalizeKey(f.FieldName),
                exampleHint = string.IsNullOrWhiteSpace(f.ExampleValue) ? null : f.ExampleValue.Trim()
            })
            .Where(f => !string.IsNullOrWhiteSpace(f.normalizedKey) && allowed.Contains(f.normalizedKey))
            .ToList();

        if (fieldDescriptors.Count == 0)
        {
            return output;
        }

        var allowedKeysJson = JsonSerializer.Serialize(fieldDescriptors.Select(f => f.normalizedKey).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
        var fieldJson = JsonSerializer.Serialize(
            fieldDescriptors.Select(f => new { key = f.normalizedKey, label = f.rawName, example = f.exampleHint }));

        var systemPrompt =
            "You extract structured business data from noisy PDF/plain text dumps. " +
            "Respond with a single JSON object only (no markdown). " +
            "Every key MUST be exactly one of the allowed keys supplied. Never invent extra keys. " +
            "If a field is genuinely absent, use empty string \"\". " +
            "Prefer verbatim values from the document for amounts/dates/email; truncate long narratives for amount-like fields.";
        var userPayload =
            "Allowed JSON keys (use these exact identifiers only):\n" +
            allowedKeysJson +
            "\n\nField definitions (hints; values must still come from the document when possible):\n" +
            fieldJson +
            "\n\nDocument text:\n" +
            TrimForModel(documentText, 24_000);

        try
        {
            var content = await CreateChatCompletionAsync(
                ApiKey!,
                Model,
                systemPrompt,
                userPayload,
                maxTokens: 2_048,
                expectJsonObject: true,
                cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(content))
            {
                return output;
            }

            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return output;
            }

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var nk = NormalizeKey(prop.Name);
                if (string.IsNullOrWhiteSpace(nk) || !allowed.Contains(nk))
                {
                    continue;
                }

                string value = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                    JsonValueKind.Number => prop.Value.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => string.Empty,
                    _ => prop.Value.ToString()
                };

                output[nk] = value.Trim();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI template extraction failed; caller may fall back to regex.");
        }

        return output;
    }

    private async Task<string?> CreateChatCompletionAsync(
        string apiKey,
        string model,
        string systemPrompt,
        string userPrompt,
        int maxTokens,
        bool expectJsonObject,
        CancellationToken cancellationToken)
    {
        var endpoint = $"{BaseUrl}/v1/chat/completions";

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new Dictionary<string, object?>
        {
            ["model"] = model,
            ["temperature"] = 0.15,
            ["max_tokens"] = maxTokens,
            ["messages"] = new object[]
            {
                new Dictionary<string, string> { ["role"] = "system", ["content"] = systemPrompt },
                new Dictionary<string, string> { ["role"] = "user", ["content"] = userPrompt }
            }
        };

        if (expectJsonObject)
        {
            payload["response_format"] = new Dictionary<string, string> { ["type"] = "json_object" };
        }

        var json = JsonSerializer.Serialize(payload);

        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException($"OpenAI {(int)response.StatusCode}: {err}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        var choices = doc.RootElement.GetProperty("choices");
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

    private static string NormalizeKey(string raw)
    {
        return System.Text.RegularExpressions.Regex.Replace(
            raw.Trim().ToLowerInvariant(),
            @"[^a-z0-9]+",
            "_").Trim('_');
    }
}

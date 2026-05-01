using email_processor_function.Models;

namespace email_processor_function.Services;

/// <summary>
/// OpenAI-backed steps for Property Hub workflows (parity with ASP.NET Document Hub helpers).
/// </summary>
public interface IOpenAiWorkflowService
{
    bool IsConfigured { get; }

    Task<string?> SummariseDocumentAsync(string documentText, string userPrompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Picks exactly one classification label from configured SQL templates (JSON: label, confidence, reason).
    /// Returns null if not configured, invalid response, or no allowed label matched — callers should fall back to heuristics.
    /// </summary>
    Task<(string Label, double Score, string Explainability)?> ClassifyWithTemplatesAsync(
        string consolidatedText,
        IReadOnlyList<ClassificationTemplate> templates,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uses the saved template fields (names + optional examples) to extract values from plain text/PDF-derived text.
    /// Returns keyed by normalized workflow token ({field:...}); invalid keys stripped.
    /// </summary>
    Task<Dictionary<string, string>> ExtractWithTemplateFieldsAsync(
        string documentText,
        IReadOnlyList<(string FieldName, string? ExampleValue)> fields,
        IReadOnlyCollection<string> allowedNormalizedKeys,
        CancellationToken cancellationToken = default);
}

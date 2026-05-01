namespace email_processor_function.Services;

/// <summary>
/// OpenAI-backed steps for Property Hub workflows (parity with ASP.NET Document Hub helpers).
/// </summary>
public interface IOpenAiWorkflowService
{
    bool IsConfigured { get; }

    Task<string?> SummariseDocumentAsync(string documentText, string userPrompt, CancellationToken cancellationToken = default);

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

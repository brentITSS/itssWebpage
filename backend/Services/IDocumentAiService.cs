using backend.DTOs;

namespace backend.Services;

public interface IDocumentAiService
{
    Task<string> SummariseAsync(string extractedText, string prompt, CancellationToken cancellationToken = default);
    Task<DocumentClassificationSuggestionDto> BuildClassificationSuggestionAsync(
        string fileName,
        string extractedText,
        CancellationToken cancellationToken = default);
}

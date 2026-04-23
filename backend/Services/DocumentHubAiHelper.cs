using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace backend.Services;

public static class DocumentHubAiHelper
{
    public static string ExtractTextFromPdf(Stream pdfStream)
    {
        try
        {
            using var document = PdfDocument.Open(pdfStream);
            var sb = new StringBuilder();
            foreach (var page in document.GetPages())
            {
                sb.AppendLine(page.Text);
            }

            var raw = sb.ToString();
            return NormalizeWhitespace(raw);
        }
        catch
        {
            return string.Empty;
        }
    }

    public static string NormalizeWhitespace(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        return Regex.Replace(input, "\\s+", " ").Trim();
    }

    public static string BuildTwoWordLabel(string fileName, string extractedText)
    {
        var source = string.IsNullOrWhiteSpace(extractedText)
            ? Path.GetFileNameWithoutExtension(fileName)
            : extractedText;

        var words = Regex.Matches(source.ToLowerInvariant(), "[a-z0-9]+")
            .Select(m => m.Value)
            .Where(w => w.Length > 2)
            .Where(w => !StopWords.Contains(w))
            .GroupBy(w => w)
            .OrderByDescending(g => g.Count())
            .ThenByDescending(g => g.Key.Length)
            .Select(g => g.Key)
            .Take(2)
            .ToList();

        if (words.Count == 0) return "General Doc";
        if (words.Count == 1) return ToTitle(words[0]);
        return $"{ToTitle(words[0])} {ToTitle(words[1])}";
    }

    public static string BuildDescription(string label, string extractedText)
    {
        if (string.IsNullOrWhiteSpace(extractedText))
        {
            return $"Documents matching the {label} category, including image-heavy or scanned files.";
        }

        var preview = extractedText.Length > 220 ? extractedText[..220] + "..." : extractedText;
        return $"Documents related to {label}. Context sample: {preview}";
    }

    public static string BuildClassificationPrompt(string label, string description)
    {
        return $"Classify a new document as '{label}' when its text and visual structure align with this description: {description}";
    }

    public static string BuildSummary(string extractedText, string prompt)
    {
        if (string.IsNullOrWhiteSpace(extractedText))
        {
            return "No extractable text was found in the uploaded PDF. Try a text-based PDF or add OCR support for scanned pages.";
        }

        var maxWords = ExtractWordLimit(prompt, 80);
        var cleanText = NormalizeWhitespace(extractedText);
        var docType = DetectDocumentType(cleanText);

        var keywords = Regex.Matches(cleanText.ToLowerInvariant(), "[a-z]{4,}")
            .Select(m => m.Value)
            .Where(w => !StopWords.Contains(w))
            .GroupBy(w => w)
            .OrderByDescending(g => g.Count())
            .ThenByDescending(g => g.Key.Length)
            .Select(g => g.Key)
            .Take(4)
            .ToList();

        var amounts = Regex.Matches(cleanText, @"(?:R|£|\$|€)?\s?\d[\d,]*(?:\.\d{2})?")
            .Select(m => m.Value.Trim())
            .Where(v => v.Length > 2)
            .Distinct()
            .Take(2)
            .ToList();

        var references = Regex.Matches(cleanText, @"(?:invoice|account|reference|ref|customer)\s*(?:no\.?|number|id)?[:#]?\s*[A-Z0-9\-]{4,}")
            .Select(m => m.Value.Trim())
            .Distinct()
            .Take(2)
            .ToList();

        var summaryParts = new List<string>
        {
            $"This {docType} focuses on {string.Join(", ", keywords.DefaultIfEmpty("key service and payment details"))}."
        };

        if (amounts.Count > 0)
        {
            summaryParts.Add($"It includes amounts such as {string.Join(" and ", amounts)}.");
        }

        if (references.Count > 0)
        {
            summaryParts.Add($"It references {string.Join(" and ", references)}.");
        }

        var draftSummary = string.Join(" ", summaryParts);
        var finalSummary = TruncateToWordLimit(draftSummary, maxWords);

        // Fallback: if heuristic summary becomes too generic, use first words from document but still obey word limit.
        if (finalSummary.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 12)
        {
            finalSummary = TruncateToWordLimit(cleanText, maxWords);
        }

        return finalSummary;
    }

    private static string ToTitle(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        return char.ToUpperInvariant(value[0]) + value[1..];
    }

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the","and","for","with","from","that","this","you","your","was","are","have","has","not","but","all",
        "its","our","can","into","out","who","when","where","what","why","how","pdf","page","pages","document",
        "please","contact","details","information","services","city","town","www","http","https","com","org","gov"
    };

    private static int ExtractWordLimit(string prompt, int fallback)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return fallback;
        }

        var match = Regex.Match(prompt, @"(\d{1,3})\s*words?", RegexOptions.IgnoreCase);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out var value))
        {
            return fallback;
        }

        return Math.Clamp(value, 20, 300);
    }

    private static string TruncateToWordLimit(string text, int maxWords)
    {
        var words = text
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(maxWords)
            .ToList();

        if (words.Count == 0)
        {
            return string.Empty;
        }

        var result = string.Join(" ", words);
        if (!result.EndsWith("."))
        {
            result += ".";
        }

        return result;
    }

    private static string DetectDocumentType(string text)
    {
        var lower = text.ToLowerInvariant();
        if (lower.Contains("invoice")) return "invoice document";
        if (lower.Contains("statement")) return "statement";
        if (lower.Contains("contract") || lower.Contains("agreement")) return "agreement";
        if (lower.Contains("notice")) return "notice";
        if (lower.Contains("report")) return "report";
        if (lower.Contains("receipt")) return "receipt";
        return "document";
    }
}

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

        var sentences = Regex.Split(extractedText, @"(?<=[\.\!\?])\s+")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Take(5);
        var body = string.Join(" ", sentences);
        return $"Prompt intent: {prompt}\n\nSummary:\n{body}";
    }

    private static string ToTitle(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        return char.ToUpperInvariant(value[0]) + value[1..];
    }

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the","and","for","with","from","that","this","you","your","was","are","have","has","not","but","all",
        "its","our","can","into","out","who","when","where","what","why","how","pdf","page","pages","document"
    };
}

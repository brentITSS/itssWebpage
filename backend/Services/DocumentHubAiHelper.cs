using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
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
            return NormalizeWhitespacePreservingLines(raw);
        }
        catch
        {
            return string.Empty;
        }
    }

    public static string ExtractTextFromXlsx(Stream xlsxStream)
    {
        try
        {
            using var document = SpreadsheetDocument.Open(xlsxStream, false);
            var workbookPart = document.WorkbookPart;
            if (workbookPart?.Workbook?.Sheets == null)
            {
                return string.Empty;
            }

            var sharedStrings = workbookPart.SharedStringTablePart?.SharedStringTable;
            var sb = new StringBuilder();
            var sheetCount = 0;

            foreach (var sheet in workbookPart.Workbook.Sheets.Elements<Sheet>())
            {
                if (sheetCount++ >= 20)
                {
                    break;
                }

                if (sheet.Id?.Value == null)
                {
                    continue;
                }

                if (workbookPart.GetPartById(sheet.Id.Value) is not WorksheetPart worksheetPart)
                {
                    continue;
                }

                var sheetData = worksheetPart.Worksheet?.GetFirstChild<SheetData>();
                if (sheetData == null)
                {
                    continue;
                }

                sb.AppendLine($"Sheet: {sheet.Name?.Value ?? "Sheet"}");

                var rowCount = 0;
                foreach (var row in sheetData.Elements<Row>())
                {
                    if (rowCount++ >= 2000)
                    {
                        break;
                    }

                    var line = FormatSpreadsheetRow(row, sharedStrings);
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        sb.AppendLine(line);
                    }
                }

                sb.AppendLine();
            }

            return NormalizeWhitespacePreservingLines(sb.ToString());
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

    /// <summary>
    /// Collapses horizontal whitespace per line but keeps line breaks so line-anchored field extraction (?m^FieldName\s*:) still works after PDF/text extraction.
    /// </summary>
    public static string NormalizeWhitespacePreservingLines(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var normalized = input.Replace("\r\n", "\n").Replace('\r', '\n');
        var sb = new StringBuilder(capacity: Math.Min(normalized.Length, 512 * 1024));
        foreach (var line in normalized.Split('\n'))
        {
            var collapsed = Regex.Replace(line, @"[ \t]+", " ").Trim();
            if (collapsed.Length > 0)
            {
                sb.AppendLine(collapsed);
            }
        }

        return sb.ToString().TrimEnd();
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

    private static string FormatSpreadsheetRow(Row row, SharedStringTable? sharedStrings)
    {
        var cells = row.Elements<Cell>()
            .Select(cell => (ColumnIndex: GetSpreadsheetColumnIndex(cell.CellReference?.Value), Text: GetSpreadsheetCellText(cell, sharedStrings)))
            .Where(cell => cell.ColumnIndex > 0)
            .OrderBy(cell => cell.ColumnIndex)
            .ToList();

        if (cells.Count == 0)
        {
            return string.Empty;
        }

        var values = new string[cells[^1].ColumnIndex];
        foreach (var (columnIndex, text) in cells)
        {
            values[columnIndex - 1] = text;
        }

        while (values.Length > 0 && string.IsNullOrWhiteSpace(values[^1]))
        {
            Array.Resize(ref values, values.Length - 1);
        }

        return values.Length == 0 ? string.Empty : string.Join(" | ", values);
    }

    private static string GetSpreadsheetCellText(Cell cell, SharedStringTable? sharedStrings)
    {
        if (cell.InlineString?.Text != null)
        {
            return cell.InlineString.Text.Text?.Trim() ?? string.Empty;
        }

        if (cell.CellValue == null)
        {
            return string.Empty;
        }

        var raw = cell.CellValue.InnerText?.Trim() ?? string.Empty;
        if (cell.DataType?.Value == CellValues.SharedString &&
            sharedStrings != null &&
            int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sharedIndex))
        {
            return sharedStrings.ElementAtOrDefault(sharedIndex)?.InnerText?.Trim() ?? raw;
        }

        return raw;
    }

    private static int GetSpreadsheetColumnIndex(string? cellReference)
    {
        if (string.IsNullOrWhiteSpace(cellReference))
        {
            return 0;
        }

        var columnLetters = new string(cellReference.TakeWhile(char.IsLetter).ToArray());
        if (columnLetters.Length == 0)
        {
            return 0;
        }

        var index = 0;
        foreach (var letter in columnLetters)
        {
            index = (index * 26) + (char.ToUpperInvariant(letter) - 'A' + 1);
        }

        return index;
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

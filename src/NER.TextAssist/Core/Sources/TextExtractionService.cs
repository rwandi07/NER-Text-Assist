using System.IO;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.VisualBasic.FileIO;
using Word = DocumentFormat.OpenXml.Wordprocessing;

namespace NER.TextAssist.Core.Sources;

public sealed class TextExtractionService
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".docx", ".csv", ".xlsx"
    };

    private static readonly HashSet<string> IgnoredWordVisualContainers = new(StringComparer.OrdinalIgnoreCase)
    {
        "drawing", "pict", "object", "txbxContent"
    };

    public bool IsSupported(string path) =>
        SupportedExtensions.Contains(Path.GetExtension(path));

    public async Task<string> ExtractAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("File sumber tidak ditemukan.", path);
        }

        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (!SupportedExtensions.Contains(extension))
        {
            throw new NotSupportedException($"Format {extension} belum didukung.");
        }

        var text = extension switch
        {
            ".txt" => await File.ReadAllTextAsync(path, cancellationToken),
            ".csv" => await Task.Run(() => ExtractCsv(path), cancellationToken),
            ".docx" => await Task.Run(() => ExtractDocx(path), cancellationToken),
            ".xlsx" => await Task.Run(() => ExtractXlsx(path), cancellationToken),
            _ => string.Empty
        };

        return Normalize(text);
    }

    private static string ExtractDocx(string path)
    {
        using var document = WordprocessingDocument.Open(path, false);
        var mainPart = document.MainDocumentPart;
        var body = mainPart?.Document?.Body;
        if (mainPart is null || body is null)
        {
            return string.Empty;
        }

        var sections = new List<string>();

        // Header/footer juga merupakan teks dokumen. Konten visual di dalamnya tetap diabaikan.
        foreach (var headerPart in mainPart.HeaderParts)
        {
            var headerText = headerPart.Header is null
                ? string.Empty
                : ExtractWordContainer(headerPart.Header);

            AddUniqueSection(sections, headerText);
        }

        AddUniqueSection(sections, ExtractWordContainer(body));

        foreach (var footerPart in mainPart.FooterParts)
        {
            var footerText = footerPart.Footer is null
                ? string.Empty
                : ExtractWordContainer(footerPart.Footer);

            AddUniqueSection(sections, footerText);
        }

        return string.Join(Environment.NewLine, sections);
    }

    private static string ExtractWordContainer(OpenXmlElement root)
    {
        var output = new StringBuilder();
        AppendWordBlocks(root, output);
        return output.ToString().Trim();
    }

    private static void AppendWordBlocks(OpenXmlElement container, StringBuilder output)
    {
        foreach (var child in container.ChildElements)
        {
            if (IgnoredWordVisualContainers.Contains(child.LocalName))
            {
                continue;
            }

            if (child is Word.Paragraph paragraph)
            {
                AppendOutputLine(output, ExtractWordParagraph(paragraph));
                continue;
            }

            if (child is Word.Table table)
            {
                AppendWordTable(table, output);
                continue;
            }

            // Content controls dan wrapper Word lain dapat memuat paragraph/table.
            AppendWordBlocks(child, output);
        }
    }

    private static void AppendWordTable(Word.Table table, StringBuilder output)
    {
        foreach (var row in table.Elements<Word.TableRow>())
        {
            var values = row.Elements<Word.TableCell>()
                .Select(ExtractWordTableCell)
                .ToList();

            while (values.Count > 0 && string.IsNullOrWhiteSpace(values[^1]))
            {
                values.RemoveAt(values.Count - 1);
            }

            if (values.Any(value => !string.IsNullOrWhiteSpace(value)))
            {
                // Tab mempertahankan batas antarsel tanpa memasukkan simbol buatan ke corpus.
                AppendOutputLine(output, string.Join("\t", values));
            }
        }
    }

    private static string ExtractWordTableCell(Word.TableCell cell)
    {
        var parts = cell
            .Descendants<Word.Paragraph>()
            .Where(paragraph => !IsInsideIgnoredVisualContainer(paragraph))
            .Select(ExtractWordParagraph)
            .Where(text => !string.IsNullOrWhiteSpace(text));

        return string.Join(" ", parts).Trim();
    }

    private static void AppendOutputLine(StringBuilder output, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        output.AppendLine(text.TrimEnd());
    }

    private static string ExtractWordParagraph(Word.Paragraph paragraph)
    {
        var output = new StringBuilder();

        foreach (var child in paragraph.ChildElements)
        {
            AppendWordElementText(child, output);
        }

        return output.ToString().Trim();
    }

    private static void AppendWordElementText(OpenXmlElement element, StringBuilder output)
    {
        if (IgnoredWordVisualContainers.Contains(element.LocalName))
        {
            return;
        }

        if (element is Word.Text text)
        {
            output.Append(text.Text);
            return;
        }

        switch (element.LocalName)
        {
            // Paragraph.InnerText menghilangkan tab Word. Ini yang sebelumnya dapat
            // menghasilkan teks seperti "Menimbang:Bahwa".
            case "tab":
            case "ptab":
                AppendSpace(output);
                return;

            case "br":
            case "cr":
                AppendLineBreak(output);
                return;

            case "noBreakHyphen":
                output.Append('-');
                return;
        }

        foreach (var child in element.ChildElements)
        {
            AppendWordElementText(child, output);
        }
    }

    private static bool IsInsideIgnoredVisualContainer(OpenXmlElement element)
    {
        var ancestor = element.Parent;
        while (ancestor is not null)
        {
            if (IgnoredWordVisualContainers.Contains(ancestor.LocalName))
            {
                return true;
            }

            ancestor = ancestor.Parent;
        }

        return false;
    }

    private static void AppendSpace(StringBuilder output)
    {
        if (output.Length > 0 && !char.IsWhiteSpace(output[^1]))
        {
            output.Append(' ');
        }
    }

    private static void AppendLineBreak(StringBuilder output)
    {
        if (output.Length == 0)
        {
            return;
        }

        if (output[^1] != '\n' && output[^1] != '\r')
        {
            output.AppendLine();
        }
    }

    private static void AddUniqueSection(List<string> sections, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (!sections.Contains(text, StringComparer.Ordinal))
        {
            sections.Add(text);
        }
    }

    private static string ExtractXlsx(string path)
    {
        using var document = SpreadsheetDocument.Open(path, false);
        var workbookPart = document.WorkbookPart;
        if (workbookPart is null)
        {
            return string.Empty;
        }

        var sheets = workbookPart.Workbook.Sheets;
        if (sheets is null)
        {
            return string.Empty;
        }

        var sharedStrings = workbookPart.SharedStringTablePart?.SharedStringTable;
        var output = new StringBuilder();

        foreach (var sheet in sheets.Elements<Sheet>())
        {
            var relationshipId = sheet.Id?.Value;
            if (string.IsNullOrWhiteSpace(relationshipId))
            {
                continue;
            }

            if (workbookPart.GetPartById(relationshipId) is not WorksheetPart worksheetPart)
            {
                continue;
            }

            foreach (var row in worksheetPart.Worksheet.Descendants<Row>())
            {
                var values = ExtractXlsxRow(row, sharedStrings);
                if (values.Count == 0 || values.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                // Tab menjaga batas kolom, tetapi tetap dianggap whitespace oleh engine teks nanti.
                output.AppendLine(string.Join("\t", values));
            }

            output.AppendLine();
        }

        return output.ToString();
    }

    private static List<string> ExtractXlsxRow(Row row, SharedStringTable? sharedStrings)
    {
        var values = new List<string>();
        var expectedColumn = 1;

        foreach (var cell in row.Elements<Cell>())
        {
            var column = GetColumnIndex(cell.CellReference?.Value);
            if (column <= 0)
            {
                column = expectedColumn;
            }

            while (expectedColumn < column)
            {
                values.Add(string.Empty);
                expectedColumn++;
            }

            values.Add(GetCellText(cell, sharedStrings).Trim());
            expectedColumn = column + 1;
        }

        while (values.Count > 0 && string.IsNullOrWhiteSpace(values[^1]))
        {
            values.RemoveAt(values.Count - 1);
        }

        return values;
    }

    private static int GetColumnIndex(string? cellReference)
    {
        if (string.IsNullOrWhiteSpace(cellReference))
        {
            return 0;
        }

        var index = 0;
        foreach (var character in cellReference)
        {
            if (!char.IsLetter(character))
            {
                break;
            }

            index = (index * 26) + (char.ToUpperInvariant(character) - 'A' + 1);
        }

        return index;
    }

    private static string GetCellText(Cell cell, SharedStringTable? sharedStrings)
    {
        if (cell.DataType?.Value == CellValues.SharedString &&
            int.TryParse(cell.CellValue?.Text, out var sharedStringIndex) &&
            sharedStrings is not null)
        {
            var item = sharedStrings.Elements<SharedStringItem>().ElementAtOrDefault(sharedStringIndex);
            return item?.InnerText ?? string.Empty;
        }

        if (cell.DataType?.Value == CellValues.InlineString)
        {
            return cell.InlineString?.InnerText ?? string.Empty;
        }

        return cell.CellValue?.Text ?? cell.InnerText ?? string.Empty;
    }

    private static string ExtractCsv(string path)
    {
        var delimiter = DetectDelimiter(path);
        var output = new StringBuilder();

        using var parser = new TextFieldParser(path)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = true
        };
        parser.SetDelimiters(delimiter);

        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields();
            if (fields is null)
            {
                continue;
            }

            var values = fields
                .Select(value => value?.Trim() ?? string.Empty)
                .ToList();

            while (values.Count > 0 && string.IsNullOrWhiteSpace(values[^1]))
            {
                values.RemoveAt(values.Count - 1);
            }

            if (values.Any(value => !string.IsNullOrWhiteSpace(value)))
            {
                output.AppendLine(string.Join("\t", values));
            }
        }

        return output.ToString();
    }

    private static string DetectDelimiter(string path)
    {
        var sampleLines = File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Take(20)
            .ToArray();

        if (sampleLines.Length == 0)
        {
            return ",";
        }

        var candidates = new[] { ',', ';', '\t' };
        var best = candidates
            .Select(candidate => new
            {
                Delimiter = candidate,
                Score = sampleLines.Sum(line => CountDelimiterOutsideQuotes(line, candidate))
            })
            .OrderByDescending(result => result.Score)
            .First();

        return best.Score == 0 ? "," : best.Delimiter.ToString();
    }

    private static int CountDelimiterOutsideQuotes(string line, char delimiter)
    {
        var count = 0;
        var insideQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (insideQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    index++;
                    continue;
                }

                insideQuotes = !insideQuotes;
                continue;
            }

            if (!insideQuotes && character == delimiter)
            {
                count++;
            }
        }

        return count;
    }

    private static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var normalized = text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Replace('\u00A0', ' ')
            .Replace("\u00AD", string.Empty, StringComparison.Ordinal);

        var lines = normalized
            .Split('\n')
            .Select(line => line.TrimEnd());

        return string.Join(Environment.NewLine, lines).Trim();
    }
}

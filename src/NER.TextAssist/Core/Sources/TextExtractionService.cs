using System.IO;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.VisualBasic.FileIO;

namespace NER.TextAssist.Core.Sources;

public sealed class TextExtractionService
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".docx", ".csv", ".xlsx"
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
        var body = document.MainDocumentPart?.Document?.Body;
        if (body is null)
        {
            return string.Empty;
        }

        var lines = body
            .Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>()
            .Select(paragraph => paragraph.InnerText.Trim())
            .Where(text => !string.IsNullOrWhiteSpace(text));

        return string.Join(Environment.NewLine, lines);
    }

    private static string ExtractXlsx(string path)
    {
        using var document = SpreadsheetDocument.Open(path, false);
        var workbookPart = document.WorkbookPart;
        if (workbookPart?.Workbook?.Sheets is null)
        {
            return string.Empty;
        }

        var sharedStrings = workbookPart.SharedStringTablePart?.SharedStringTable;
        var output = new StringBuilder();

        foreach (var sheet in workbookPart.Workbook.Sheets.Elements<Sheet>())
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
                var values = row.Elements<Cell>()
                    .Select(cell => GetCellText(cell, sharedStrings))
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToArray();

                if (values.Length > 0)
                {
                    output.AppendLine(string.Join(" ", values));
                }
            }

            output.AppendLine();
        }

        return output.ToString();
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
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim())
                .ToArray();

            if (values.Length > 0)
            {
                output.AppendLine(string.Join(" ", values));
            }
        }

        return output.ToString();
    }

    private static string DetectDelimiter(string path)
    {
        var firstLine = File.ReadLines(path)
            .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line)) ?? string.Empty;

        var candidates = new[] { ",", ";", "\t" };
        return candidates
            .OrderByDescending(candidate => firstLine.Count(character => character == candidate[0]))
            .First();
    }

    private static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var normalized = text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

        var lines = normalized
            .Split('\n')
            .Select(line => line.TrimEnd());

        return string.Join(Environment.NewLine, lines).Trim();
    }
}

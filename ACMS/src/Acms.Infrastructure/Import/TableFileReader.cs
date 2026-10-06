using System.Globalization;
using System.Text;
using Acms.Core.Import;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using HtmlAgilityPack;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using Wp = DocumentFormat.OpenXml.Wordprocessing;

namespace Acms.Infrastructure.Import;

/// <summary>
/// Reads the update list out of an uploaded file. Every format is reduced to rows of text cells and
/// <see cref="ImportTable.Best"/> picks the table that holds the machines.
/// </summary>
public static class TableFileReader
{
    public static readonly IReadOnlyList<string> SupportedExtensions =
        [".xlsx", ".xlsm", ".csv", ".tsv", ".txt", ".docx", ".pdf", ".eml", ".msg", ".htm", ".html"];

    /// <exception cref="ImportException">The file type is not supported or the file cannot be read.</exception>
    public static ImportTable Read(Stream stream, string fileName)
    {
        var name = Path.GetFileName(fileName);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (!SupportedExtensions.Contains(extension))
        {
            throw new ImportException(extension is ".xls" or ".doc"
                ? $"{name} is an old Office file. Save it as {(extension == ".xls" ? ".xlsx" : ".docx")} and upload again."
                : $"{name}: this file type is not supported. Use Excel (.xlsx), CSV, Word (.docx), PDF or an e-mail (.eml, .msg).");
        }

        // The Office, PDF and Outlook readers need a seekable stream.
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        buffer.Position = 0;

        try
        {
            var tables = extension switch
            {
                ".xlsx" or ".xlsm" => ReadExcel(buffer),
                ".docx" => ReadWord(buffer),
                ".pdf" => ReadPdf(buffer),
                ".eml" => ReadEml(buffer),
                ".msg" => ReadMsg(buffer),
                ".htm" or ".html" => ReadHtml(ReadText(buffer)),
                _ => [ReadDelimited(ReadText(buffer))],
            };

            return ImportTable.Best(name, tables);
        }
        catch (Exception ex) when (ex is not ImportException)
        {
            throw new ImportException($"{name} could not be read: {ex.Message}", ex);
        }
    }

    // ---------- Excel (.xlsx): every worksheet is a table ----------

    private static List<List<IReadOnlyList<string?>>> ReadExcel(Stream stream)
    {
        using var document = SpreadsheetDocument.Open(stream, false);
        var workbook = document.WorkbookPart ?? throw new ImportException("The workbook is empty.");
        var shared = workbook.SharedStringTablePart?.SharedStringTable?.Elements<SharedStringItem>()
            .Select(i => i.InnerText).ToArray() ?? [];

        var tables = new List<List<IReadOnlyList<string?>>>();
        foreach (var sheet in workbook.Workbook?.Sheets?.Elements<Sheet>() ?? [])
        {
            if (sheet.Id?.Value is not { } id || workbook.GetPartById(id) is not WorksheetPart part)
            {
                continue;
            }

            var rows = new List<IReadOnlyList<string?>>();
            foreach (var row in part.Worksheet?.Descendants<Row>() ?? [])
            {
                var cells = new List<string?>();
                foreach (var cell in row.Elements<Cell>())
                {
                    var column = ColumnIndex(cell.CellReference?.Value) ?? cells.Count;
                    while (cells.Count < column)
                    {
                        cells.Add(string.Empty);
                    }

                    cells.Add(CellText(cell, shared));
                }

                rows.Add(cells);
            }

            tables.Add(rows);
        }

        return tables;
    }

    private static string CellText(Cell cell, string[] shared)
    {
        var raw = cell.CellValue?.Text ?? string.Empty;
        var type = cell.DataType?.Value;

        if (type == CellValues.SharedString)
        {
            return int.TryParse(raw, out var index) && index < shared.Length ? shared[index] : string.Empty;
        }

        if (type == CellValues.InlineString)
        {
            return cell.InlineString?.InnerText ?? string.Empty;
        }

        if (type == CellValues.Boolean)
        {
            return raw == "1" ? "TRUE" : "FALSE";
        }

        // Numbers are stored as doubles: 96.8 can come back as 96.799999999999997.
        if ((type is null || type == CellValues.Number)
            && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            return number.ToString("G15", CultureInfo.InvariantCulture);
        }

        return raw;
    }

    private static int? ColumnIndex(string? reference)
    {
        if (string.IsNullOrEmpty(reference))
        {
            return null;
        }

        var index = 0;
        foreach (var c in reference.TakeWhile(char.IsLetter))
        {
            index = index * 26 + (char.ToUpperInvariant(c) - 'A' + 1);
        }

        return index - 1;
    }

    // ---------- Word (.docx): every table; plain paragraphs when there is none ----------

    private static List<List<IReadOnlyList<string?>>> ReadWord(Stream stream)
    {
        using var document = WordprocessingDocument.Open(stream, false);
        var body = document.MainDocumentPart?.Document?.Body ?? throw new ImportException("The document is empty.");

        var tables = new List<List<IReadOnlyList<string?>>>();
        foreach (var table in body.Descendants<Wp.Table>())
        {
            var rows = new List<IReadOnlyList<string?>>();
            foreach (var tr in table.Elements<Wp.TableRow>())
            {
                var cells = new List<string?>();
                foreach (var tc in tr.Elements<Wp.TableCell>())
                {
                    cells.Add(string.Join(" ", tc.Elements<Wp.Paragraph>().Select(p => p.InnerText)));
                    var span = tc.TableCellProperties?.GridSpan?.Val?.Value ?? 1;
                    cells.AddRange(Enumerable.Repeat<string?>(string.Empty, Math.Max(0, span - 1)));
                }

                rows.Add(cells);
            }

            tables.Add(rows);
        }

        if (tables.Count == 0)
        {
            var text = string.Join("\n", body.Elements<Wp.Paragraph>().Select(p => p.InnerText));
            tables.Add(ImportText.SplitPasted(text));
        }

        return tables;
    }

    // ---------- PDF: text lines, split into cells at wide gaps ----------

    private static List<List<IReadOnlyList<string?>>> ReadPdf(Stream stream)
    {
        using var document = PdfDocument.Open(stream);
        var rows = new List<IReadOnlyList<string?>>();

        foreach (var page in document.GetPages())
        {
            var words = page.GetWords().Where(w => !string.IsNullOrWhiteSpace(w.Text)).ToList();

            // Group words into lines by their baseline, top of the page first.
            var lines = new List<List<Word>>();
            foreach (var word in words.OrderByDescending(w => w.BoundingBox.Bottom).ThenBy(w => w.BoundingBox.Left))
            {
                var line = lines.LastOrDefault();
                var tolerance = Math.Max(2, word.BoundingBox.Height * 0.5);
                if (line is null || Math.Abs(line[0].BoundingBox.Bottom - word.BoundingBox.Bottom) > tolerance)
                {
                    lines.Add([word]);
                }
                else
                {
                    line.Add(word);
                }
            }

            foreach (var line in lines)
            {
                rows.Add(SplitPdfLine(line.OrderBy(w => w.BoundingBox.Left).ToList()));
            }
        }

        return [rows];
    }

    /// <summary>Words closer than about one and a half characters belong to the same cell ("MCD 1220").</summary>
    private static List<string?> SplitPdfLine(List<Word> words)
    {
        var cells = new List<string?>();
        var current = new StringBuilder(words[0].Text);

        for (var i = 1; i < words.Count; i++)
        {
            var previous = words[i - 1];
            var charWidth = previous.BoundingBox.Width / Math.Max(1, previous.Text.Length);
            var gap = words[i].BoundingBox.Left - previous.BoundingBox.Right;

            if (gap > Math.Max(2, charWidth * 1.5))
            {
                cells.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(' ');
            }

            current.Append(words[i].Text);
        }

        cells.Add(current.ToString());
        return cells;
    }

    // ---------- E-mail (.eml, .msg) and HTML: the tables in the message ----------

    private static List<List<IReadOnlyList<string?>>> ReadEml(Stream stream)
    {
        var message = MsgReader.Mime.Message.Load(stream);
        return ReadMessageBody(message.HtmlBody?.GetBodyAsText(), message.TextBody?.GetBodyAsText());
    }

    private static List<List<IReadOnlyList<string?>>> ReadMsg(Stream stream)
    {
        using var message = new MsgReader.Outlook.Storage.Message(stream, FileAccess.Read, false);
        return ReadMessageBody(message.BodyHtml, message.BodyText);
    }

    private static List<List<IReadOnlyList<string?>>> ReadMessageBody(string? html, string? text)
    {
        var tables = string.IsNullOrWhiteSpace(html) ? [] : ReadHtml(html);
        return tables.Count > 0 ? tables : [ImportText.SplitPasted(text)];
    }

    private static List<List<IReadOnlyList<string?>>> ReadHtml(string html)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html);

        var tables = new List<List<IReadOnlyList<string?>>>();
        foreach (var table in document.DocumentNode.SelectNodes("//table") ?? Enumerable.Empty<HtmlNode>())
        {
            var rows = new List<IReadOnlyList<string?>>();
            foreach (var tr in table.SelectNodes("./tr|./thead/tr|./tbody/tr|./tfoot/tr") ?? Enumerable.Empty<HtmlNode>())
            {
                var cells = new List<string?>();
                foreach (var cell in tr.SelectNodes("./td|./th") ?? Enumerable.Empty<HtmlNode>())
                {
                    cells.Add(HtmlEntity.DeEntitize(cell.InnerText));
                    var span = cell.GetAttributeValue("colspan", 1);
                    cells.AddRange(Enumerable.Repeat<string?>(string.Empty, Math.Clamp(span - 1, 0, 50)));
                }

                rows.Add(cells);
            }

            tables.Add(rows);
        }

        return tables;
    }

    // ---------- CSV / TSV / text ----------

    private static string ReadText(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    /// <summary>Tab, semicolon or comma separated, whichever the first line uses most; quotes are honoured.</summary>
    private static List<IReadOnlyList<string?>> ReadDelimited(string text)
    {
        var firstLine = text.Split('\n').FirstOrDefault(l => l.Trim().Length > 0) ?? string.Empty;
        var delimiter = new[] { '\t', ';', ',' }.MaxBy(d => firstLine.Count(c => c == d));
        if (firstLine.All(c => c is not ('\t' or ';' or ',')))
        {
            return ImportText.SplitPasted(text);
        }

        var rows = new List<IReadOnlyList<string?>>();
        var cells = new List<string?>();
        var cell = new StringBuilder();
        var quoted = false;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            if (quoted)
            {
                if (c == '"' && i + 1 < text.Length && text[i + 1] == '"')
                {
                    cell.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    quoted = false;
                }
                else
                {
                    cell.Append(c);
                }
            }
            else if (c == '"' && cell.Length == 0)
            {
                quoted = true;
            }
            else if (c == delimiter)
            {
                cells.Add(cell.ToString());
                cell.Clear();
            }
            else if (c == '\n')
            {
                cells.Add(cell.ToString().TrimEnd('\r'));
                cell.Clear();
                rows.Add(cells);
                cells = [];
            }
            else
            {
                cell.Append(c);
            }
        }

        if (cell.Length > 0 || cells.Count > 0)
        {
            cells.Add(cell.ToString().TrimEnd('\r'));
            rows.Add(cells);
        }

        return rows;
    }
}

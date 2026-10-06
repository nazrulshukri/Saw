using System.Text;
using Acms.Core.Import;
using Acms.Infrastructure.Import;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using Wp = DocumentFormat.OpenXml.Wordprocessing;

namespace Acms.Tests;

public class TableFileReaderTests
{
    private static ImportTable Read(byte[] content, string fileName) =>
        TableFileReader.Read(new MemoryStream(content), fileName);

    private static ImportTable Read(string content, string fileName) => Read(Encoding.UTF8.GetBytes(content), fileName);

    private static void AssertIeRow(ImportTable table, string expectedPackage = "Micropack")
    {
        Assert.True(table.HasHeader);
        Assert.Equal("Ws", table.Headers[0]);
        Assert.Equal("Change T Speed", table.Headers[^1]);
        Assert.Equal("DB-AXF-012S", table.Rows[0][0]);
        Assert.Equal(expectedPackage, table.Rows[0][1]);
        Assert.Equal("28000", table.Rows[0][^1]);
    }

    [Fact]
    public void Reads_csv_with_quotes_and_semicolons()
    {
        AssertIeRow(Read("Ws,Package,Change T Speed\r\nDB-AXF-012S,\"Micropack\",28000\r\n", "uph.csv"));

        var semicolon = Read("WSID;CONTROL\nDB-AXF-013S;\"ATX18II,Flex\"\n", "list.csv");
        Assert.Equal("ATX18II,Flex", semicolon.Rows[0][1]);
    }

    [Fact]
    public void Reads_an_Excel_workbook_with_shared_strings_gaps_and_float_noise()
    {
        using var stream = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbook = document.AddWorkbookPart();
            workbook.Workbook = new Workbook();

            var strings = workbook.AddNewPart<SharedStringTablePart>();
            strings.SharedStringTable = new SharedStringTable(new SharedStringItem(new Text("Change T Speed")));

            static Cell Text(string reference, string value) =>
                new() { CellReference = reference, DataType = CellValues.InlineString, InlineString = new InlineString(new Text(value)) };

            var sheet = workbook.AddNewPart<WorksheetPart>();
            sheet.Worksheet = new Worksheet(new SheetData(
                new Row(Text("A1", "UPH by package")) { RowIndex = 1 },
                new Row(Text("A2", "Ws"), Text("B2", "Package"), Text("C2", "Oee%"),
                    new Cell { CellReference = "E2", DataType = CellValues.SharedString, CellValue = new CellValue("0") }) { RowIndex = 2 },
                new Row(Text("A3", "DB-AXF-012S"), Text("B3", "Micropack"),
                    new Cell { CellReference = "C3", CellValue = new CellValue("96.799999999999997") },
                    new Cell { CellReference = "E3", CellValue = new CellValue("28000") }) { RowIndex = 3 }));

            workbook.Workbook.AppendChild(new Sheets(new Sheet { Id = workbook.GetIdOfPart(sheet), SheetId = 1, Name = "UPH" }));
        }

        var table = Read(stream.ToArray(), "UPH.xlsx");

        AssertIeRow(table);
        Assert.Equal("Column 4", table.Headers[3]);
        Assert.Equal("96.8", table.Rows[0][2]);
    }

    [Fact]
    public void Reads_a_Word_table_with_merged_cells()
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            static Wp.TableCell Cell(string text, int span = 1) => new(
                new Wp.TableCellProperties(new Wp.GridSpan { Val = span }),
                new Wp.Paragraph(new Wp.Run(new Wp.Text(text))));

            var main = document.AddMainDocumentPart();
            main.Document = new Wp.Document(new Wp.Body(
                new Wp.Paragraph(new Wp.Run(new Wp.Text("Please update:"))),
                new Wp.Table(
                    new Wp.TableRow(Cell("Period", 2), Cell("")),
                    new Wp.TableRow(Cell("Ws"), Cell("Package"), Cell("Change T Speed")),
                    new Wp.TableRow(Cell("DB-AXF-012S"), Cell("Micropack"), Cell("28000")))));
        }

        AssertIeRow(Read(stream.ToArray(), "request.docx"));
    }

    [Fact]
    public void Reads_the_table_from_an_eml_e_mail_and_ignores_the_signature_table()
    {
        const string eml = """
            From: IE <ie@example.com>
            To: Eng <eng@example.com>
            Subject: UPH by Package and Machine
            MIME-Version: 1.0
            Content-Type: text/html; charset=utf-8

            <html><body><p>Please update:</p>
            <table><tr><td colspan="3">Period</td></tr>
            <tr><th>Ws</th><th>Package</th><th>Change T Speed</th></tr>
            <tr><td>&nbsp;&nbsp; DB-AXF-012S</td><td>Micropack</td><td>28000</td></tr></table>
            <table><tr><td>Aziim Azhar</td><td>FAP-Equipment Automation</td></tr></table>
            </body></html>
            """;

        AssertIeRow(Read(eml.Replace("\n", "\r\n"), "UPH.eml"));
    }

    [Fact]
    public void Reads_a_pdf_table_keeping_words_of_one_cell_together()
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        var page = builder.AddPage(PageSize.A4);

        void Line(double y, params (double X, string Text)[] cells)
        {
            foreach (var (x, text) in cells)
            {
                page.AddText(text, 10, new PdfPoint(x, y), font);
            }
        }

        Line(760, (50, "UPH list"));
        Line(740, (50, "Ws"), (160, "Package"), (260, "Change T Speed"));
        Line(725, (50, "DB-AXF-012S"), (160, "MCD 1220"), (260, "28000"));

        AssertIeRow(Read(builder.Build(), "uph.pdf"), expectedPackage: "MCD 1220");
    }

    [Theory]
    [InlineData("old.xls", ".xlsx")]
    [InlineData("old.doc", ".docx")]
    [InlineData("photo.png", "not supported")]
    public void Refuses_unsupported_files_with_a_helpful_message(string fileName, string expected)
    {
        var ex = Assert.Throws<ImportException>(() => Read([1, 2, 3], fileName));

        Assert.Contains(expected, ex.Message);
    }

    [Fact]
    public void Reports_a_damaged_file_as_an_import_error()
    {
        var ex = Assert.Throws<ImportException>(() => Read([1, 2, 3], "broken.xlsx"));

        Assert.Contains("broken.xlsx", ex.Message);
    }
}

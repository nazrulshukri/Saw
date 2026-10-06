using Acms.Core.Services;

namespace Acms.Tests;

public class BulkInputParserTests
{
    [Fact]
    public void Takes_first_and_last_cell_of_rows_pasted_from_an_email_table()
    {
        // Columns: Ws, Package, State, Prod%, Oee%, Tspeed, Speed, Total, Change T Speed
        const string text =
            "Ws\tPackage\tState\tProd%\tOee%\tTspeed\tSpeed\tTotal\tChange T Speed\r\n" +
            "   DB-AXF-012S\tMicropack\tMWAIT\t0\t0\t25600\t0\t0\t28000\r\n" +
            "   DB-AXF-006S\tMCD 1220\tIDLE\t12.4\t11.3\t32000\t29041\t10336\tN/A\r\n" +
            "\r\n" +
            "DB-AXF-013S\tPicogate\tPROD\t86.7\t114.6\t36800\t48638\t123458\t46000\t\r\n";

        var lines = BulkInputParser.Parse(text);

        Assert.Equal(3, lines.Count);
        Assert.Equal(new BulkInputLine(2, "DB-AXF-012S", "28000"), lines[0]);
        Assert.Equal(new BulkInputLine(3, "DB-AXF-006S", null), lines[1]);
        Assert.Equal(new BulkInputLine(5, "DB-AXF-013S", "46000"), lines[2]);
    }

    [Theory]
    [InlineData("DB-AXF-012S 28000", "28000")]
    [InlineData("DB-AXF-012S,28000", "28000")]
    [InlineData("DB-AXF-012S;28000", "28000")]
    [InlineData("DB-AXF-013S   ATX18II,Flex", "ATX18II,Flex")]
    [InlineData("DB-AXF-012S 28000", "28000")]
    [InlineData("DB-AXF-012S", null)]
    [InlineData("DB-AXF-012S -", null)]
    public void Splits_typed_lines_at_the_first_separator(string line, string? expectedValue)
    {
        var parsed = Assert.Single(BulkInputParser.Parse(line));

        Assert.Equal(line.StartsWith("DB-AXF-013S") ? "DB-AXF-013S" : "DB-AXF-012S", parsed.WsId);
        Assert.Equal(expectedValue, parsed.Value);
    }

    [Fact]
    public void Returns_nothing_for_empty_input()
    {
        Assert.Empty(BulkInputParser.Parse(null));
        Assert.Empty(BulkInputParser.Parse(" \n \n"));
    }
}

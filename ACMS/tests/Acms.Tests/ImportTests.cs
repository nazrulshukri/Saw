using Acms.Core.Import;

namespace Acms.Tests;

public class ImportTests
{
    private static readonly ImportOptions Options = new()
    {
        ColumnAliases = new(StringComparer.OrdinalIgnoreCase) { ["Change T Speed"] = "SPEED_SPEC" },
    };

    private static readonly string[] Known = ["SPEED_SPEC", "LOCATION", "AREA"];

    // The IE UPH table: a title row above the real header, then one row per machine.
    private static readonly string?[][] IeTable =
    [
        ["Period", "Package", "3x Hours", "Upto", "10/6/2026 9:00", "Remark"],
        ["Ws", "", "State", "Prod%", "Oee%", "Tspeed", "Speed", "Total", "Change T Speed"],
        ["  DB-AXF-012S", "Micropack", "MWAIT", "0", "0", "25600", "0", "0", "28000"],
        ["DB-AXF-006S", "MCD 1220", "IDLE", "12.4", "11.3", "32000", "29041", "10336", "N/A"],
    ];

    [Fact]
    public void Finds_the_header_row_below_title_rows_and_cleans_cells()
    {
        var table = ImportTable.FromRows("mail", IeTable);

        Assert.True(table.HasHeader);
        Assert.Equal(["Ws", "Column 2", "State", "Prod%", "Oee%", "Tspeed", "Speed", "Total", "Change T Speed"], table.Headers);
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("DB-AXF-012S", table.Rows[0][0]);
        Assert.Equal(0, table.WsIdColumn);
    }

    [Fact]
    public void Suggests_WSID_aliases_and_known_attribute_names_only()
    {
        var table = ImportTable.FromRows("mail", IeTable);

        var mapping = ColumnMapper.Suggest(table, Options, Known);

        // Tspeed (the old value), State, Speed... are ignored; "Change T Speed" is the new SPEED_SPEC.
        Assert.Equal(["WSID", "", "", "", "", "", "", "", "SPEED_SPEC"], mapping);
    }

    [Fact]
    public void Matches_titles_written_differently_and_never_maps_two_columns_to_one_attribute()
    {
        var table = ImportTable.FromRows("x", [["Location", "WSID", "speed spec", "Speed-Spec"], ["PH3C", "DB-AXF-012S", "1", "2"]]);

        Assert.Equal(["LOCATION", "WSID", "SPEED_SPEC", ""], ColumnMapper.Suggest(table, Options, Known));
    }

    [Fact]
    public void Turns_rows_into_machines_with_N_A_as_no_value()
    {
        var table = ImportTable.FromRows("mail", IeTable);

        var (rows, errors) = ColumnMapper.ToRows(table, ColumnMapper.Suggest(table, Options, Known));

        Assert.Empty(errors);
        Assert.Equal(2, rows.Count);
        Assert.Equal("28000", rows[0].Values["SPEED_SPEC"]);
        Assert.Null(rows[1].Values["speed_spec"]);
    }

    [Fact]
    public void Skips_header_rows_repeated_on_later_pages()
    {
        var table = ImportTable.FromRows("pdf", [["Ws", "SPEED_SPEC"], ["A-1", "1"], ["Ws", "SPEED_SPEC"], ["B-2", "2"]]);

        var (rows, _) = ColumnMapper.ToRows(table, ["WSID", "SPEED_SPEC"]);

        Assert.Equal(["A-1", "B-2"], rows.Select(r => r.WsId));
    }

    [Theory]
    [InlineData(new[] { "", "SPEED_SPEC" }, "exactly one column as WSID")]
    [InlineData(new[] { "WSID", "" }, "at least one column")]
    [InlineData(new[] { "WSID", "BAD NAME" }, "not a valid attribute")]
    public void Reports_mapping_problems(string[] mapping, string expected)
    {
        var table = ImportTable.FromRows("x", [["Ws", "Value"], ["A-1", "1"]]);

        var (rows, errors) = ColumnMapper.ToRows(table, mapping);

        Assert.Empty(rows);
        Assert.Contains(errors, e => e.Contains(expected));
    }

    [Fact]
    public void Without_a_title_row_the_first_column_is_the_machine_and_the_rest_is_chosen_by_the_user()
    {
        var table = ImportTable.Best("Pasted text", [ImportText.SplitPasted("DB-AXF-012S 28000\nDB-AXF-013S\t46000\t\n")]);

        Assert.False(table.HasHeader);
        Assert.Equal(["Column 1", "Column 2"], table.Headers);
        Assert.Equal(["WSID", ""], ColumnMapper.Suggest(table, Options, Known));
    }

    [Fact]
    public void Best_prefers_the_table_with_a_machine_header_over_signature_tables()
    {
        string?[][] signature = [["Aziim Azhar", "FAP-Equipment Automation"], ["Phone", "+60 1234"]];

        var table = ImportTable.Best("mail", [signature, IeTable]);

        Assert.Equal("Ws", table.Headers[0]);
    }

    [Theory]
    [InlineData(" N/A ", null)]
    [InlineData("-", null)]
    [InlineData("", null)]
    [InlineData(" 28000 ", "28000")]
    public void Normalizes_values(string value, string? expected)
    {
        Assert.Equal(expected, ImportText.NormalizeValue(value));
    }
}

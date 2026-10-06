using Acms.Core.Services;

namespace Acms.Tests;

public class AttributeDiffTests
{
    private static readonly Dictionary<string, string> Current = new(StringComparer.OrdinalIgnoreCase)
    {
        ["RECIPELOAD"] = "RCP_01",
        ["STATE"] = " IDLE ",
    };

    [Fact]
    public void Changes_returns_only_values_that_differ()
    {
        var requested = new Dictionary<string, string>
        {
            ["recipeload"] = "RCP_02",
            ["STATE"] = "IDLE",
            ["NEWATTR"] = "1",
        };

        var changes = AttributeDiff.Changes(Current, requested);

        Assert.Equal(2, changes.Count);
        Assert.Equal("RCP_02", changes["RECIPELOAD"]);
        Assert.Equal("1", changes["NEWATTR"]);
    }

    [Fact]
    public void Changes_keeps_the_requested_value_exactly()
    {
        var changes = AttributeDiff.Changes(Current, new Dictionary<string, string> { ["TOP_LINE_1"] = "B7t,DB09,639, " });

        Assert.Equal("B7t,DB09,639, ", changes["TOP_LINE_1"]);
    }

    [Fact]
    public void Treats_a_missing_attribute_as_empty()
    {
        // AWACS leaves empty attributes out of wsdata.xml.
        Assert.Empty(AttributeDiff.Changes(Current, new Dictionary<string, string> { ["AREA"] = " " }));
        Assert.Empty(AttributeDiff.Verify(Current, new Dictionary<string, string> { ["AREA"] = "" }));
    }

    [Fact]
    public void Verify_reports_values_that_were_not_applied()
    {
        var expected = new Dictionary<string, string> { ["RECIPELOAD"] = "RCP_01", ["MISSING"] = "x" };

        var mismatches = AttributeDiff.Verify(Current, expected);

        var mismatch = Assert.Single(mismatches);
        Assert.Equal("MISSING", mismatch.Name);
        Assert.Null(mismatch.Actual);
    }
}

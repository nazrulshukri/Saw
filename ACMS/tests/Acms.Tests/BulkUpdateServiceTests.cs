using Acms.Core.Domain;
using Acms.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Acms.Tests;

public class BulkUpdateServiceTests
{
    private readonly InMemoryServerRepository _servers = new();
    private readonly ScriptedAwacsClient _awacs = new();
    private readonly InMemoryAuditLog _audit = new();
    private readonly EquipmentRulesOptions _rules = new() { KnownAttributes = ["SPEED_SPEC"] };

    public BulkUpdateServiceTests()
    {
        _servers.Servers.Add(new AwacsServer { Id = 1, Name = "MS079", BaseUrl = "http://ms079/" });
        _servers.Servers.Add(new AwacsServer { Id = 2, Name = "MS080", BaseUrl = "http://ms080/" });
        _servers.Servers.Add(new AwacsServer { Id = 3, Name = "OLD", BaseUrl = "http://old/", IsActive = false });

        _awacs.Add(1, "DB-AXF-012S", ("SPEED_SPEC", "25600"));
        _awacs.Add(1, "DB-AXF-013S", ("SPEED_SPEC", "46000"));
        _awacs.Add(1, "DB-AXF-014S", ("MODEL", "XF_DBSG"));
        _awacs.Add(2, "DB-AD3-103S", ("SPEED_SPEC", "25600"));
        _awacs.Add(1, "DB-TWIN-001", ("SPEED_SPEC", "1"));
        _awacs.Add(2, "DB-TWIN-001", ("SPEED_SPEC", "1"));
        _awacs.Add(3, "DB-OLD-001", ("SPEED_SPEC", "1"));
    }

    private BulkUpdateService CreateService()
    {
        var options = Options.Create(_rules);
        var changes = new EquipmentChangeService(_servers, _awacs, _audit, options, TimeProvider.System,
            NullLogger<EquipmentChangeService>.Instance);
        return new BulkUpdateService(_servers, _awacs, changes, options, NullLogger<BulkUpdateService>.Instance);
    }

    private static List<BulkInputLine> Lines(params (string WsId, string? Value)[] lines) =>
        lines.Select((l, i) => new BulkInputLine(i + 1, l.WsId, l.Value)).ToList();

    [Fact]
    public async Task Preview_locates_each_machine_and_classifies_it_without_changing_AWACS()
    {
        var preview = await CreateService().PreviewAsync("SPEED_SPEC", Lines(
            ("DB-AXF-012S", "28000"),
            ("db-ad3-103s", "28000"),
            ("DB-AXF-013S", "46000"),
            ("DB-AXF-006S", null),
            ("DB-AXF-012S", "30000"),
            ("DB-NOPE-001", "1"),
            ("DB-TWIN-001", "2"),
            ("DB-OLD-001", "2"),
            ("bad id!", "1"),
            ("DB-AXF-014S", "42000"),
            ("DB-AXF-013S\u0000", "1")));

        Assert.True(preview.IsValid);
        Assert.Equal(
            [
                BulkRowStatus.Change, BulkRowStatus.Change, BulkRowStatus.NoChange, BulkRowStatus.NoValue,
                BulkRowStatus.Duplicate, BulkRowStatus.NotFound, BulkRowStatus.Ambiguous, BulkRowStatus.NotFound,
                BulkRowStatus.Invalid, BulkRowStatus.Change, BulkRowStatus.Invalid,
            ],
            preview.Rows.Select(r => r.Status));

        Assert.Equal("MS079", preview.Rows[0].ServerName);
        Assert.Equal("25600", preview.Rows[0].CurrentValue);
        Assert.Equal("MS080", preview.Rows[1].ServerName);
        Assert.Null(preview.Rows[9].CurrentValue);
        Assert.Equal(0, _awacs.UpdateCalls);
        Assert.Empty(_audit.Entries);
    }

    [Fact]
    public async Task Preview_reports_unreachable_servers_instead_of_not_found()
    {
        _awacs.UnreachableServers.Add(2);

        var preview = await CreateService().PreviewAsync("SPEED_SPEC", Lines(("DB-AD3-103S", "28000"), ("DB-AXF-012S", "28000")));

        Assert.Equal([BulkRowStatus.Unknown, BulkRowStatus.Change], preview.Rows.Select(r => r.Status));
        Assert.Contains("MS080", Assert.Single(preview.ServerErrors));
    }

    [Theory]
    [InlineData("", "Enter the attribute")]
    [InlineData("WSID", "read-only")]
    public async Task Preview_rejects_invalid_attributes(string attribute, string expected)
    {
        var preview = await CreateService().PreviewAsync(attribute, Lines(("DB-AXF-012S", "1")));

        Assert.False(preview.IsValid);
        Assert.Contains(expected, Assert.Single(preview.Errors));
        Assert.Empty(preview.Rows);
    }

    [Fact]
    public async Task Preview_rejects_lists_that_are_too_long()
    {
        var lines = Enumerable.Range(1, BulkUpdateService.MaxLines + 1).Select(i => ($"WS-{i}", (string?)"1")).ToArray();

        var preview = await CreateService().PreviewAsync("SPEED_SPEC", Lines(lines));

        Assert.False(preview.IsValid);
    }

    [Fact]
    public async Task Apply_changes_verifies_and_audits_each_machine()
    {
        var service = CreateService();
        var preview = await service.PreviewAsync("SPEED_SPEC", Lines(("DB-AXF-012S", "28000"), ("DB-AD3-103S", "28000")));
        var items = preview.Rows.Select(r => new BulkApplyItem(r.ServerId!.Value, r.WsId, r.CurrentValue, r.NewValue!)).ToList();

        var results = await service.ApplyAsync("SPEED_SPEC", items, @"COMPANY\eng1");

        Assert.All(results, r => Assert.Equal(AuditOutcome.Success, r.Result.Outcome));
        Assert.Equal(["MS079", "MS080"], results.Select(r => r.ServerName));
        Assert.Equal("28000", _awacs.For(1)["DB-AXF-012S"]["SPEED_SPEC"]);
        Assert.Equal("28000", _awacs.For(2)["DB-AD3-103S"]["SPEED_SPEC"]);
        Assert.Equal(2, _audit.Entries.Count);
        Assert.All(_audit.Entries, e => Assert.Equal(@"COMPANY\eng1", e.UserName));
    }

    [Fact]
    public async Task Apply_refuses_a_machine_that_changed_after_the_preview()
    {
        var service = CreateService();
        var preview = await service.PreviewAsync("SPEED_SPEC", Lines(("DB-AXF-012S", "28000")));
        _awacs.For(1)["DB-AXF-012S"]["SPEED_SPEC"] = "30000";

        var row = preview.Rows[0];
        var result = Assert.Single(await service.ApplyAsync("SPEED_SPEC",
            [new BulkApplyItem(row.ServerId!.Value, row.WsId, row.CurrentValue, row.NewValue!)], "user"));

        Assert.True(result.Result.IsConflict);
        Assert.Equal("30000", _awacs.For(1)["DB-AXF-012S"]["SPEED_SPEC"]);
    }
}

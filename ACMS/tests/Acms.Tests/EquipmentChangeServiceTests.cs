using Acms.Core.Domain;
using Acms.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Acms.Tests;

public class EquipmentChangeServiceTests
{
    private const string WsId = "RM-ELM-001";

    private readonly InMemoryServerRepository _servers = new();
    private readonly ScriptedAwacsClient _awacs = new();
    private readonly InMemoryAuditLog _audit = new();
    private readonly EquipmentRulesOptions _rules = new();

    public EquipmentChangeServiceTests()
    {
        _servers.Servers.Add(new AwacsServer { Id = 1, Name = "AWACS-1", BaseUrl = "http://awacs1/" });
        _servers.Servers.Add(new AwacsServer { Id = 2, Name = "AWACS-OLD", BaseUrl = "http://awacs2/", IsActive = false });

        _awacs.Stations[WsId] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["WSID"] = WsId,
            ["RECIPELOAD"] = "RCP_01",
            ["TOP_LINE_1"] = "B7t,DB09,639, ",
        };
    }

    private EquipmentChangeService CreateService() =>
        new(_servers, _awacs, _audit, Options.Create(_rules), TimeProvider.System, NullLogger<EquipmentChangeService>.Instance);

    private static EquipmentEditRequest Request(
        Dictionary<string, string> values,
        Dictionary<string, string>? expectedOriginal = null,
        int serverId = 1,
        string wsId = WsId) =>
        new(serverId, wsId, values, expectedOriginal);

    [Fact]
    public async Task Applies_only_changed_values_verifies_and_audits()
    {
        var result = await CreateService().EditAsync(
            Request(new() { ["RECIPELOAD"] = "RCP_02", ["TOP_LINE_1"] = "B7t,DB09,639, " }),
            @"COMPANY\engineer1");

        Assert.Equal(AuditOutcome.Success, result.Outcome);
        Assert.True(result.AuditWritten);
        Assert.Equal(["RECIPELOAD"], result.Changes.Keys);
        Assert.Equal("RCP_01", result.Before["RECIPELOAD"]);
        Assert.Equal("RCP_02", result.After["RECIPELOAD"]);
        Assert.Equal(1, _awacs.UpdateCalls);
        Assert.Equal("RCP_02", _awacs.Stations[WsId]["RECIPELOAD"]);

        var entry = Assert.Single(_audit.Entries);
        Assert.Equal(AuditAction.EquipmentEdit, entry.Action);
        Assert.Equal(AuditOutcome.Success, entry.Outcome);
        Assert.Equal(@"COMPANY\engineer1", entry.UserName);
        Assert.Equal("AWACS-1", entry.ServerName);
        Assert.Equal(WsId, entry.WsId);
        Assert.Equal(result.CorrelationId, entry.CorrelationId);
        Assert.Contains("RCP_01", entry.BeforeJson);
        Assert.Contains("RCP_02", entry.AfterJson);
        Assert.DoesNotContain("TOP_LINE_1", entry.BeforeJson);
    }

    [Fact]
    public async Task Sends_nothing_when_values_are_already_set()
    {
        var result = await CreateService().EditAsync(Request(new() { ["recipeload"] = " RCP_01 " }), "user");

        Assert.Equal(AuditOutcome.NoChange, result.Outcome);
        Assert.True(result.Succeeded);
        Assert.Equal(0, _awacs.UpdateCalls);
        Assert.Equal(AuditOutcome.NoChange, Assert.Single(_audit.Entries).Outcome);
    }

    [Theory]
    [InlineData("WSID", "OTHER", "read-only")]
    [InlineData("BAD NAME", "x", "invalid")]
    [InlineData("RECIPELOAD", "line1\nline2", "control characters")]
    public async Task Rejects_invalid_values_without_contacting_AWACS(string name, string value, string expectedMessage)
    {
        var result = await CreateService().EditAsync(Request(new() { [name] = value }), "user");

        Assert.Equal(AuditOutcome.Rejected, result.Outcome);
        Assert.Contains(expectedMessage, result.Message);
        Assert.Equal(0, _awacs.UpdateCalls);
        Assert.Equal(AuditOutcome.Rejected, Assert.Single(_audit.Entries).Outcome);
    }

    [Fact]
    public async Task Rejects_values_longer_than_the_configured_limit()
    {
        _rules.MaxValueLength = 5;

        var result = await CreateService().EditAsync(Request(new() { ["RECIPELOAD"] = "RCP_LONG" }), "user");

        Assert.Equal(AuditOutcome.Rejected, result.Outcome);
        Assert.Equal(0, _awacs.UpdateCalls);
    }

    [Theory]
    [InlineData("RM ELM 001")]
    [InlineData("A&setwsattr=X")]
    [InlineData("")]
    public async Task Rejects_invalid_workstation_ids(string wsId)
    {
        var result = await CreateService().EditAsync(Request(new() { ["RECIPELOAD"] = "RCP_02" }, wsId: wsId), "user");

        Assert.Equal(AuditOutcome.Rejected, result.Outcome);
        Assert.Equal(0, _awacs.UpdateCalls);
    }

    [Fact]
    public async Task Rejects_empty_requests()
    {
        var result = await CreateService().EditAsync(Request([]), "user");

        Assert.Equal(AuditOutcome.Rejected, result.Outcome);
    }

    [Theory]
    [InlineData(99, "does not exist")]
    [InlineData(2, "inactive")]
    public async Task Rejects_missing_or_inactive_servers(int serverId, string expectedMessage)
    {
        var result = await CreateService().EditAsync(Request(new() { ["RECIPELOAD"] = "RCP_02" }, serverId: serverId), "user");

        Assert.Equal(AuditOutcome.Rejected, result.Outcome);
        Assert.Contains(expectedMessage, result.Message);
        Assert.Equal(0, _awacs.UpdateCalls);
    }

    [Fact]
    public async Task Rejects_unknown_workstation()
    {
        var result = await CreateService().EditAsync(Request(new() { ["RECIPELOAD"] = "RCP_02" }, wsId: "NOPE-001"), "user");

        Assert.Equal(AuditOutcome.Rejected, result.Outcome);
        Assert.Contains("not found", result.Message);
    }

    [Fact]
    public async Task Rejects_new_attributes_unless_allowed()
    {
        var rejected = await CreateService().EditAsync(Request(new() { ["RECIPELAOD"] = "RCP_02" }), "user");

        Assert.Equal(AuditOutcome.Rejected, rejected.Outcome);
        Assert.Contains("does not exist", rejected.Message);
        Assert.Equal(0, _awacs.UpdateCalls);

        _rules.AllowNewAttributes = true;
        var allowed = await CreateService().EditAsync(Request(new() { ["NEWATTR"] = "1" }), "user");

        Assert.Equal(AuditOutcome.Success, allowed.Outcome);
    }

    [Fact]
    public async Task Allows_known_attributes_that_AWACS_did_not_return_because_they_are_empty()
    {
        _rules.KnownAttributes = ["SPEED_SPEC"];

        var result = await CreateService().EditAsync(
            Request(new() { ["speed_spec"] = "46000" }, expectedOriginal: new() { ["SPEED_SPEC"] = "" }),
            "user");

        Assert.Equal(AuditOutcome.Success, result.Outcome);
        Assert.Equal("46000", _awacs.Stations[WsId]["speed_spec"]);
    }

    [Fact]
    public async Task Clearing_a_value_is_verified_when_AWACS_stops_returning_it()
    {
        var result = await CreateService().EditAsync(Request(new() { ["RECIPELOAD"] = "" }), "user");

        Assert.Equal(AuditOutcome.Success, result.Outcome);
        Assert.False(_awacs.Stations[WsId].ContainsKey("RECIPELOAD"));
    }

    [Fact]
    public async Task Rejects_double_quotes_because_setwsattr_cannot_carry_them()
    {
        var result = await CreateService().EditAsync(Request(new() { ["RECIPELOAD"] = "RCP \"A\"" }), "user");

        Assert.Equal(AuditOutcome.Rejected, result.Outcome);
        Assert.Contains("double quote", result.Message);
        Assert.Equal(0, _awacs.UpdateCalls);
    }

    [Fact]
    public async Task Rejects_when_AWACS_changed_after_the_form_was_loaded()
    {
        var result = await CreateService().EditAsync(
            Request(new() { ["RECIPELOAD"] = "RCP_02" }, expectedOriginal: new() { ["RECIPELOAD"] = "RCP_00" }),
            "user");

        Assert.Equal(AuditOutcome.Rejected, result.Outcome);
        Assert.True(result.IsConflict);
        Assert.Contains("RECIPELOAD", result.Message);
        Assert.Equal(0, _awacs.UpdateCalls);
    }

    [Fact]
    public async Task Ignores_whitespace_when_checking_for_conflicts()
    {
        var result = await CreateService().EditAsync(
            Request(new() { ["TOP_LINE_1"] = "B7t,DB09,640, " }, expectedOriginal: new() { ["top_line_1"] = "B7t,DB09,639," }),
            "user");

        Assert.Equal(AuditOutcome.Success, result.Outcome);
        Assert.Equal("B7t,DB09,640, ", _awacs.Stations[WsId]["TOP_LINE_1"]);
    }

    [Fact]
    public async Task Fails_when_the_reread_does_not_show_the_new_value()
    {
        _awacs.IgnoreUpdates = true;

        var result = await CreateService().EditAsync(Request(new() { ["RECIPELOAD"] = "RCP_02" }), "user");

        Assert.Equal(AuditOutcome.Failed, result.Outcome);
        var mismatch = Assert.Single(result.Mismatches);
        Assert.Equal("RCP_02", mismatch.Expected);
        Assert.Equal("RCP_01", mismatch.Actual);
        Assert.Contains("Not applied", result.Message);

        var entry = Assert.Single(_audit.Entries);
        Assert.Equal(AuditOutcome.Failed, entry.Outcome);
        Assert.Contains("RCP_01", entry.AfterJson);
    }

    [Fact]
    public async Task Fails_and_still_rereads_when_the_update_call_throws()
    {
        _awacs.ThrowOnUpdate = new HttpRequestException("connection reset");

        var result = await CreateService().EditAsync(Request(new() { ["RECIPELOAD"] = "RCP_02" }), "user");

        Assert.Equal(AuditOutcome.Failed, result.Outcome);
        Assert.Contains("connection reset", result.Message);
        Assert.Equal("RCP_01", result.After["RECIPELOAD"]);
        Assert.NotNull(Assert.Single(_audit.Entries).AfterJson);
    }

    [Fact]
    public async Task Fails_without_updating_when_the_before_read_throws()
    {
        _awacs.ThrowOnRead = new InvalidOperationException("AWACS down");

        var result = await CreateService().EditAsync(Request(new() { ["RECIPELOAD"] = "RCP_02" }), "user");

        Assert.Equal(AuditOutcome.Failed, result.Outcome);
        Assert.Contains("AWACS down", result.Message);
        Assert.Equal(0, _awacs.UpdateCalls);
        Assert.Single(_audit.Entries);
    }

    [Fact]
    public async Task Reports_a_lost_audit_row_without_hiding_the_change()
    {
        _audit.FailWrites = true;

        var result = await CreateService().EditAsync(Request(new() { ["RECIPELOAD"] = "RCP_02" }), "user");

        Assert.Equal(AuditOutcome.Success, result.Outcome);
        Assert.False(result.AuditWritten);
        Assert.Equal("RCP_02", _awacs.Stations[WsId]["RECIPELOAD"]);
    }
}

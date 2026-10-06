using Acms.Core.Domain;
using Acms.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Acms.Tests;

public class ServerAdminServiceTests
{
    private readonly InMemoryServerRepository _servers = new();
    private readonly InMemoryAuditLog _audit = new();

    private ServerAdminService CreateService() =>
        new(_servers, _audit, TimeProvider.System, NullLogger<ServerAdminService>.Instance);

    [Fact]
    public async Task Create_normalizes_input_and_audits()
    {
        var result = await CreateService().CreateAsync(
            new ServerInput("  AWACS ATSN Line 1 ", " http://awacs01.company.local/awacs ", "  "),
            "admin");

        Assert.True(result.Succeeded);
        var server = Assert.Single(_servers.Servers);
        Assert.Equal("AWACS ATSN Line 1", server.Name);
        Assert.Equal("http://awacs01.company.local/awacs/", server.BaseUrl);
        Assert.Null(server.Description);
        Assert.True(server.IsActive);
        Assert.Equal("admin", server.CreatedBy);

        var entry = Assert.Single(_audit.Entries);
        Assert.Equal(AuditAction.ServerCreate, entry.Action);
        Assert.Null(entry.BeforeJson);
        Assert.Contains("awacs01", entry.AfterJson);
    }

    [Fact]
    public async Task Create_keeps_only_the_site_root_of_a_pasted_page_address()
    {
        var result = await CreateService().CreateAsync(
            new ServerInput("MS073", "http://myser01ms073.nws.nexperia.com:8080/template/general/status.html/", null), "admin");

        Assert.Equal("http://myser01ms073.nws.nexperia.com:8080/", result.Server!.BaseUrl);
    }

    [Theory]
    [InlineData("")]
    [InlineData("awacs01")]
    [InlineData("ftp://awacs01/")]
    [InlineData("http://awacs01/?ws=*")]
    [InlineData("http://awacs01/#top")]
    public async Task Create_rejects_invalid_base_urls(string baseUrl)
    {
        var result = await CreateService().CreateAsync(new ServerInput("AWACS", baseUrl, null), "admin");

        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Empty(_servers.Servers);
        Assert.Empty(_audit.Entries);
    }

    [Fact]
    public async Task Create_rejects_missing_and_too_long_fields()
    {
        var result = await CreateService().CreateAsync(
            new ServerInput("", "http://awacs01/", new string('x', 501)), "admin");

        Assert.False(result.Succeeded);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public async Task Create_rejects_duplicate_names()
    {
        var service = CreateService();
        await service.CreateAsync(new ServerInput("AWACS-1", "http://awacs01/", null), "admin");

        var result = await service.CreateAsync(new ServerInput("AWACS-1", "http://awacs02/", null), "admin");

        Assert.False(result.Succeeded);
        Assert.Contains("already exists", Assert.Single(result.Errors));
    }

    [Fact]
    public async Task Update_keeps_its_own_name_and_records_before_and_after()
    {
        var service = CreateService();
        var created = (await service.CreateAsync(new ServerInput("AWACS-1", "http://awacs01/", null), "admin")).Server!;

        var result = await service.UpdateAsync(created.Id, new ServerInput("AWACS-1", "http://awacs01-new/", "moved"), "admin2");

        Assert.True(result.Succeeded);
        Assert.Equal("http://awacs01-new/", created.BaseUrl);
        Assert.Equal("admin2", created.UpdatedBy);

        var entry = _audit.Entries[^1];
        Assert.Equal(AuditAction.ServerUpdate, entry.Action);
        Assert.Contains("http://awacs01/", entry.BeforeJson);
        Assert.Contains("http://awacs01-new/", entry.AfterJson);
    }

    [Fact]
    public async Task SetActive_audits_only_real_changes()
    {
        var service = CreateService();
        var created = (await service.CreateAsync(new ServerInput("AWACS-1", "http://awacs01/", null), "admin")).Server!;

        await service.SetActiveAsync(created.Id, true, "admin");
        Assert.Single(_audit.Entries);

        var result = await service.SetActiveAsync(created.Id, false, "admin");

        Assert.True(result.Succeeded);
        Assert.False(created.IsActive);
        Assert.Equal(AuditAction.ServerDeactivate, _audit.Entries[^1].Action);
    }

    [Fact]
    public async Task Update_and_SetActive_fail_for_unknown_servers()
    {
        var service = CreateService();

        Assert.False((await service.UpdateAsync(42, new ServerInput("A", "http://a/", null), "admin")).Succeeded);
        Assert.False((await service.SetActiveAsync(42, false, "admin")).Succeeded);
    }
}

using Acms.Core.Abstractions;
using Acms.Core.Domain;
using Acms.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Acms.Tests;

public sealed class EfAuditLogTests : IDisposable
{
    private static readonly DateTime Day = new(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly DbContextOptions<AcmsDbContext> _options;

    public EfAuditLogTests()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<AcmsDbContext>().UseSqlite(_connection).Options;

        using var db = new AcmsDbContext(_options);
        db.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    private async Task SeedAsync(params AuditEntry[] entries)
    {
        await using var db = new AcmsDbContext(_options);
        var log = new EfAuditLog(db);
        foreach (var entry in entries)
        {
            await log.WriteAsync(entry);
        }
    }

    private async Task<PagedResult<AuditEntry>> QueryAsync(AuditQuery query)
    {
        await using var db = new AcmsDbContext(_options);
        return await new EfAuditLog(db).QueryAsync(query);
    }

    private static AuditEntry Entry(int hour, string wsId, string user = "COMPANY\\eng1", AuditOutcome outcome = AuditOutcome.Success) => new()
    {
        TimestampUtc = Day.AddHours(hour),
        UserName = user,
        Action = AuditAction.EquipmentEdit,
        Outcome = outcome,
        ServerId = 1,
        WsId = wsId,
        CorrelationId = Guid.NewGuid().ToString("N"),
    };

    [Fact]
    public async Task Returns_newest_first_with_paging()
    {
        await SeedAsync(Entry(1, "A"), Entry(3, "B"), Entry(2, "C"));

        var page1 = await QueryAsync(new AuditQuery { PageSize = 2 });
        var page2 = await QueryAsync(new AuditQuery { PageSize = 2, Page = 2 });

        Assert.Equal(3, page1.TotalCount);
        Assert.Equal(2, page1.TotalPages);
        Assert.Equal(["B", "C"], page1.Items.Select(a => a.WsId));
        Assert.Equal("A", Assert.Single(page2.Items).WsId);
    }

    [Fact]
    public async Task Filters_by_workstation_user_outcome_and_time()
    {
        await SeedAsync(
            Entry(1, "RM-ELM-001"),
            Entry(2, "RM-ELM-002", user: "COMPANY\\eng2"),
            Entry(3, "RM-ELM-001", outcome: AuditOutcome.Failed),
            Entry(30, "RM-ELM-001"));

        Assert.Equal(3, (await QueryAsync(new AuditQuery { WsId = "ELM-001" })).TotalCount);
        Assert.Equal(1, (await QueryAsync(new AuditQuery { UserName = "eng2" })).TotalCount);
        Assert.Equal(1, (await QueryAsync(new AuditQuery { Outcome = AuditOutcome.Failed })).TotalCount);
        Assert.Equal(2, (await QueryAsync(new AuditQuery { FromUtc = Day.AddHours(2), ToUtc = Day.AddDays(1) })).TotalCount);
    }

    [Fact]
    public async Task Reads_timestamps_back_as_UTC_and_truncates_long_messages()
    {
        var entry = Entry(5, "A");
        entry.Message = new string('x', 2500);
        await SeedAsync(entry);

        var stored = Assert.Single((await QueryAsync(new AuditQuery())).Items);

        Assert.Equal(DateTimeKind.Utc, stored.TimestampUtc.Kind);
        Assert.Equal(Day.AddHours(5), stored.TimestampUtc);
        Assert.Equal(2000, stored.Message!.Length);
    }
}

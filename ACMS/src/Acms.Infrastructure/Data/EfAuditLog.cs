using Acms.Core.Abstractions;
using Acms.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Acms.Infrastructure.Data;

/// <summary>Central audit trail in the ACMS SQL Server database. Rows are only ever inserted.</summary>
public sealed class EfAuditLog : IAuditLog
{
    private const int MaxPageSize = 500;

    private readonly AcmsDbContext _db;

    public EfAuditLog(AcmsDbContext db)
    {
        _db = db;
    }

    public async Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        if (entry.Message?.Length > 2000)
        {
            entry.Message = entry.Message[..2000];
        }

        _db.AuditEntries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<AuditEntry>> QueryAsync(AuditQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var rows = _db.AuditEntries.AsNoTracking();

        if (query.ServerId is not null)
        {
            rows = rows.Where(a => a.ServerId == query.ServerId);
        }

        if (!string.IsNullOrWhiteSpace(query.WsId))
        {
            var wsId = query.WsId.Trim();
            rows = rows.Where(a => a.WsId != null && a.WsId.Contains(wsId));
        }

        if (!string.IsNullOrWhiteSpace(query.UserName))
        {
            var user = query.UserName.Trim();
            rows = rows.Where(a => a.UserName.Contains(user));
        }

        if (query.Action is not null)
        {
            rows = rows.Where(a => a.Action == query.Action);
        }

        if (query.Outcome is not null)
        {
            rows = rows.Where(a => a.Outcome == query.Outcome);
        }

        if (query.FromUtc is not null)
        {
            rows = rows.Where(a => a.TimestampUtc >= query.FromUtc);
        }

        if (query.ToUtc is not null)
        {
            rows = rows.Where(a => a.TimestampUtc < query.ToUtc);
        }

        var total = await rows.CountAsync(cancellationToken);
        var items = await rows
            .OrderByDescending(a => a.TimestampUtc)
            .ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditEntry>(items, total, page, pageSize);
    }
}

using Acms.Core.Domain;

namespace Acms.Core.Abstractions;

public interface IAuditLog
{
    Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken = default);

    Task<PagedResult<AuditEntry>> QueryAsync(AuditQuery query, CancellationToken cancellationToken = default);
}

public sealed record AuditQuery
{
    public int? ServerId { get; init; }
    public string? WsId { get; init; }
    public string? UserName { get; init; }
    public AuditAction? Action { get; init; }
    public AuditOutcome? Outcome { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

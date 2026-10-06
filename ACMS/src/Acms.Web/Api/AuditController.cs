using Acms.Core.Abstractions;
using Acms.Core.Domain;
using Acms.Core.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Acms.Web.Api;

[ApiController]
[Route("api/audit")]
[Authorize(Policy = AcmsPolicies.CanView)]
public sealed class AuditController : ControllerBase
{
    private readonly IAuditLog _audit;

    public AuditController(IAuditLog audit)
    {
        _audit = audit;
    }

    /// <summary>Audit trail, newest first. Times are UTC.</summary>
    [HttpGet]
    public Task<PagedResult<AuditEntry>> Get(
        [FromQuery] int? serverId,
        [FromQuery] string? wsId,
        [FromQuery] string? user,
        [FromQuery] AuditAction? action,
        [FromQuery] AuditOutcome? outcome,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        return _audit.QueryAsync(new AuditQuery
        {
            ServerId = serverId,
            WsId = wsId,
            UserName = user,
            Action = action,
            Outcome = outcome,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            Page = page,
            PageSize = pageSize,
        }, cancellationToken);
    }
}

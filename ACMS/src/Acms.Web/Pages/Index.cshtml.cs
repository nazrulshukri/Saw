using Acms.Core.Abstractions;
using Acms.Core.Domain;
using Acms.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Acms.Web.Pages;

/// <summary>Landing page: single view of all AWACS servers and recent activity.</summary>
public class IndexModel : PageModel
{
    private readonly IServerRepository _servers;
    private readonly ServerStatusService _status;
    private readonly IAuditLog _audit;

    public IndexModel(IServerRepository servers, ServerStatusService status, IAuditLog audit)
    {
        _servers = servers;
        _status = status;
        _audit = audit;
    }

    [BindProperty(SupportsGet = true)]
    public bool Refresh { get; set; }

    public IReadOnlyList<ServerStatus> Statuses { get; private set; } = [];
    public IReadOnlyList<AuditEntry> RecentActivity { get; private set; } = [];
    public int ChangesToday { get; private set; }
    public int FailuresToday { get; private set; }

    public int ServersOnline => Statuses.Count(s => s.Reachable);
    public int TotalWorkstations => Statuses.Sum(s => s.WorkstationCount ?? 0);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var servers = await _servers.GetAllAsync(includeInactive: false, cancellationToken);
        Statuses = await _status.GetAsync(servers, Refresh, cancellationToken);

        RecentActivity = (await _audit.QueryAsync(new AuditQuery { PageSize = 10 }, cancellationToken)).Items;

        var startOfDayUtc = DateTime.Today.ToUniversalTime();
        ChangesToday = (await _audit.QueryAsync(new AuditQuery
        {
            Action = AuditAction.EquipmentEdit,
            Outcome = AuditOutcome.Success,
            FromUtc = startOfDayUtc,
            PageSize = 1,
        }, cancellationToken)).TotalCount;

        FailuresToday = (await _audit.QueryAsync(new AuditQuery
        {
            Action = AuditAction.EquipmentEdit,
            Outcome = AuditOutcome.Failed,
            FromUtc = startOfDayUtc,
            PageSize = 1,
        }, cancellationToken)).TotalCount;
    }
}

using Acms.Core.Abstractions;
using Acms.Core.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Acms.Web.Pages.Audit;

/// <summary>Audit trail review. Dates in the filter are server local time.</summary>
public class IndexModel : PageModel
{
    private readonly IAuditLog _audit;
    private readonly IServerRepository _servers;

    public IndexModel(IAuditLog audit, IServerRepository servers)
    {
        _audit = audit;
        _servers = servers;
    }

    [BindProperty(SupportsGet = true)] public int? ServerId { get; set; }
    [BindProperty(SupportsGet = true)] public string? WsId { get; set; }
    [BindProperty(SupportsGet = true)] public string? UserName { get; set; }
    [BindProperty(SupportsGet = true)] public AuditAction? Action { get; set; }
    [BindProperty(SupportsGet = true)] public AuditOutcome? Outcome { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? From { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? To { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    public IReadOnlyList<AwacsServer> Servers { get; private set; } = [];
    public PagedResult<AuditEntry> Results { get; private set; } = new([], 0, 1, 50);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Servers = await _servers.GetAllAsync(includeInactive: true, cancellationToken);

        Results = await _audit.QueryAsync(new AuditQuery
        {
            ServerId = ServerId,
            WsId = WsId,
            UserName = UserName,
            Action = Action,
            Outcome = Outcome,
            FromUtc = From?.Date.ToUniversalTime(),
            ToUtc = To?.Date.AddDays(1).ToUniversalTime(),
            Page = PageNumber,
            PageSize = 50,
        }, cancellationToken);
    }

    /// <summary>Route values for a link to another page with the same filters.</summary>
    public Dictionary<string, string> RouteFor(int page)
    {
        var values = new Dictionary<string, string> { ["pageNumber"] = page.ToString() };

        void Add(string key, object? value)
        {
            if (value is not null && !string.IsNullOrWhiteSpace(value.ToString()))
            {
                values[key] = value is DateTime d ? d.ToString("yyyy-MM-dd") : value.ToString()!;
            }
        }

        Add("serverId", ServerId);
        Add("wsId", WsId);
        Add("userName", UserName);
        Add("action", Action);
        Add("outcome", Outcome);
        Add("from", From);
        Add("to", To);
        return values;
    }
}

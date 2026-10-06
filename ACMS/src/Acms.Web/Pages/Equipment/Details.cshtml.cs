using Acms.Core.Abstractions;
using Acms.Core.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Acms.Web.Pages.Equipment;

public class DetailsModel : PageModel
{
    private readonly IServerRepository _servers;
    private readonly IAwacsClient _awacs;
    private readonly IAuditLog _audit;

    public DetailsModel(IServerRepository servers, IAwacsClient awacs, IAuditLog audit)
    {
        _servers = servers;
        _awacs = awacs;
        _audit = audit;
    }

    [BindProperty(SupportsGet = true)]
    public int ServerId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string WsId { get; set; } = string.Empty;

    public AwacsServer? Server { get; private set; }
    public Workstation? Workstation { get; private set; }
    public string? Error { get; private set; }
    public IReadOnlyList<AuditEntry> History { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        Server = await _servers.GetAsync(ServerId, cancellationToken);
        if (Server is null || string.IsNullOrWhiteSpace(WsId))
        {
            return NotFound();
        }

        try
        {
            Workstation = await _awacs.GetWorkstationAsync(Server, WsId, cancellationToken);
            if (Workstation is null)
            {
                Error = $"Workstation '{WsId}' was not found on '{Server.Name}'.";
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Error = $"Could not read from '{Server.Name}': {ex.Message}";
        }

        History = (await _audit.QueryAsync(new AuditQuery { ServerId = ServerId, WsId = WsId, PageSize = 20 }, cancellationToken))
            .Items
            .Where(a => string.Equals(a.WsId, WsId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Page();
    }
}

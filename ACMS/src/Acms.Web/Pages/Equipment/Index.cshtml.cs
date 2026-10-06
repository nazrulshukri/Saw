using Acms.Core.Abstractions;
using Acms.Core.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Acms.Web.Pages.Equipment;

/// <summary>Consolidated equipment search across one or all AWACS servers.</summary>
public class IndexModel : PageModel
{
    private readonly IServerRepository _servers;
    private readonly IAwacsClient _awacs;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IServerRepository servers, IAwacsClient awacs, IOptions<UiOptions> ui, ILogger<IndexModel> logger)
    {
        _servers = servers;
        _awacs = awacs;
        _logger = logger;
        SummaryAttributes = ui.Value.SummaryAttributes;
    }

    /// <summary>Selected server; null means all active servers.</summary>
    [BindProperty(SupportsGet = true)]
    public int? ServerId { get; set; }

    /// <summary>Matches WSID or any summary attribute value (case-insensitive).</summary>
    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    public IReadOnlyList<string> SummaryAttributes { get; }
    public IReadOnlyList<AwacsServer> Servers { get; private set; } = [];
    public List<EquipmentRow> Rows { get; } = [];
    public List<string> Errors { get; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Servers = await _servers.GetAllAsync(includeInactive: false, cancellationToken);

        var targets = ServerId is null
            ? Servers
            : Servers.Where(s => s.Id == ServerId).ToList();

        var results = await Task.WhenAll(targets.Select(async server =>
        {
            try
            {
                return (server, list: await _awacs.GetWorkstationsAsync(server, null, cancellationToken), error: (string?)null);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Could not load equipment from {Server}", server.Name);
                return (server, list: (IReadOnlyList<Workstation>)[], error: $"{server.Name}: {ex.Message}");
            }
        }));

        foreach (var (server, list, error) in results)
        {
            if (error is not null)
            {
                Errors.Add(error);
            }

            Rows.AddRange(list.Where(Matches).Select(w => new EquipmentRow(server, w)));
        }

        Rows.Sort((a, b) =>
        {
            var byServer = string.Compare(a.Server.Name, b.Server.Name, StringComparison.OrdinalIgnoreCase);
            return byServer != 0 ? byServer : string.Compare(a.Workstation.WsId, b.Workstation.WsId, StringComparison.OrdinalIgnoreCase);
        });
    }

    private bool Matches(Workstation workstation)
    {
        if (string.IsNullOrWhiteSpace(Q))
        {
            return true;
        }

        var q = Q.Trim();
        return workstation.WsId.Contains(q, StringComparison.OrdinalIgnoreCase)
            || SummaryAttributes.Any(a => workstation.Get(a)?.Contains(q, StringComparison.OrdinalIgnoreCase) == true);
    }

    public sealed record EquipmentRow(AwacsServer Server, Workstation Workstation);
}

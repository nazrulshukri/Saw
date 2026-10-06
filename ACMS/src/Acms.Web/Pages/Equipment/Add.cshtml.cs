using Acms.Core.Abstractions;
using Acms.Core.Domain;
using Acms.Core.Services;
using Acms.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Acms.Web.Pages.Equipment;

/// <summary>Adds a workstation to an AWACS server. Requires Engineer or Administrator (see Program.cs).</summary>
public class AddModel : PageModel
{
    private readonly IServerRepository _servers;
    private readonly EquipmentChangeService _changes;
    private readonly string[] _known;

    public AddModel(IServerRepository servers, EquipmentChangeService changes, IOptions<EquipmentRulesOptions> rules)
    {
        _servers = servers;
        _changes = changes;
        _known = rules.Value.KnownAttributes;
    }

    [BindProperty(SupportsGet = true)]
    public int ServerId { get; set; }

    [BindProperty]
    public string WsId { get; set; } = string.Empty;

    [BindProperty]
    public List<EditModel.AttributeInput> Attributes { get; set; } = [];

    public IReadOnlyList<AwacsServer> Servers { get; private set; } = [];
    public EquipmentChangeResult? Result { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Servers = await _servers.GetAllAsync(includeInactive: false, cancellationToken);
        Attributes = _known.Order(StringComparer.OrdinalIgnoreCase)
            .Select(n => new EditModel.AttributeInput { Name = n, Value = string.Empty })
            .ToList();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Servers = await _servers.GetAllAsync(includeInactive: false, cancellationToken);
        var values = Attributes
            .Where(a => !string.IsNullOrWhiteSpace(a.Value))
            .ToDictionary(a => a.Name, a => a.Value!, StringComparer.OrdinalIgnoreCase);

        Result = await _changes.CreateAsync(new EquipmentCreateRequest(ServerId, WsId.Trim(), values), User.AcmsUserName(), cancellationToken);
        if (Result.Outcome == AuditOutcome.Success)
        {
            return RedirectToPage("Details", new { serverId = ServerId, wsId = WsId.Trim() });
        }

        return Page();
    }
}

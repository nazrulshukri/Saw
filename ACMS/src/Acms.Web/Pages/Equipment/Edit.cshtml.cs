using Acms.Core.Abstractions;
using Acms.Core.Domain;
using Acms.Core.Services;
using Acms.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Acms.Web.Pages.Equipment;

/// <summary>Guided edit form. Requires the Engineer or Administrator role (see Program.cs).</summary>
public class EditModel : PageModel
{
    private readonly IServerRepository _servers;
    private readonly IAwacsClient _awacs;
    private readonly EquipmentChangeService _changes;
    private readonly HashSet<string> _readOnly;
    private readonly string[] _knownAttributes;

    public EditModel(
        IServerRepository servers,
        IAwacsClient awacs,
        EquipmentChangeService changes,
        IOptions<EquipmentRulesOptions> rules)
    {
        _servers = servers;
        _awacs = awacs;
        _changes = changes;
        _readOnly = new HashSet<string>(rules.Value.ReadOnlyAttributes, StringComparer.OrdinalIgnoreCase);
        _knownAttributes = rules.Value.KnownAttributes;
    }

    [BindProperty(SupportsGet = true)]
    public int ServerId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string WsId { get; set; } = string.Empty;

    [BindProperty]
    public List<AttributeInput> Attributes { get; set; } = [];

    public AwacsServer? Server { get; private set; }
    public string? Error { get; private set; }
    public string? Info { get; private set; }
    public EquipmentChangeResult? Result { get; private set; }

    public bool IsReadOnly(string name) => _readOnly.Contains(name);

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        Server = await _servers.GetAsync(ServerId, cancellationToken);
        if (Server is null)
        {
            return NotFound();
        }

        await LoadAttributesAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Server = await _servers.GetAsync(ServerId, cancellationToken);
        if (Server is null)
        {
            return NotFound();
        }

        // Send only what the user actually changed; the service diffs again against live AWACS values.
        var edited = Attributes
            .Where(a => !IsReadOnly(a.Name) && (a.Value ?? string.Empty).Trim() != (a.Original ?? string.Empty).Trim())
            .ToList();

        if (edited.Count == 0)
        {
            Info = "You did not change any value.";
            return Page();
        }

        var request = new EquipmentEditRequest(
            ServerId,
            WsId,
            edited.ToDictionary(a => a.Name, a => a.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase),
            edited.ToDictionary(a => a.Name, a => a.Original ?? string.Empty, StringComparer.OrdinalIgnoreCase));

        Result = await _changes.EditAsync(request, User.AcmsUserName(), cancellationToken);

        // Show what AWACS holds now, whatever the outcome.
        ModelState.Clear();
        await LoadAttributesAsync(cancellationToken);
        return Page();
    }

    private async Task LoadAttributesAsync(CancellationToken cancellationToken)
    {
        if (!Server!.IsActive)
        {
            Error = $"'{Server.Name}' is inactive. Equipment on it cannot be changed.";
            return;
        }

        try
        {
            var workstation = await _awacs.GetWorkstationAsync(Server, WsId, cancellationToken);
            if (workstation is null)
            {
                Error = $"Workstation '{WsId}' was not found on '{Server.Name}'.";
                Attributes = [];
                return;
            }

            // AWACS leaves empty attributes out of wsdata.xml; show the known ones anyway so they can be
            // filled in, as on the AWACS edit workstation page.
            var values = new Dictionary<string, string>(workstation.Attributes, StringComparer.OrdinalIgnoreCase);
            foreach (var name in _knownAttributes)
            {
                values.TryAdd(name, string.Empty);
            }

            Attributes = values
                .OrderBy(a => !string.Equals(a.Key, "WSID", StringComparison.OrdinalIgnoreCase))
                .ThenBy(a => a.Key, StringComparer.OrdinalIgnoreCase)
                .Select(a => new AttributeInput { Name = a.Key, Value = a.Value, Original = a.Value })
                .ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Error = $"Could not read from '{Server.Name}': {ex.Message}";
        }
    }

    public sealed class AttributeInput
    {
        public string Name { get; set; } = string.Empty;
        public string? Value { get; set; }

        /// <summary>Value when the form was loaded; used to detect concurrent changes.</summary>
        public string? Original { get; set; }
    }
}

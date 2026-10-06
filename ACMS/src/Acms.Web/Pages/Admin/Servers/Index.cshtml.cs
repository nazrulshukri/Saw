using Acms.Core.Abstractions;
using Acms.Core.Domain;
using Acms.Core.Services;
using Acms.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Acms.Web.Pages.Admin.Servers;

/// <summary>AWACS server connections. Administrator only (folder convention in Program.cs).</summary>
public class IndexModel : PageModel
{
    private readonly IServerRepository _servers;
    private readonly ServerAdminService _admin;

    public IndexModel(IServerRepository servers, ServerAdminService admin)
    {
        _servers = servers;
        _admin = admin;
    }

    public IReadOnlyList<AwacsServer> Servers { get; private set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Servers = await _servers.GetAllAsync(includeInactive: true, cancellationToken);
    }

    public async Task<IActionResult> OnPostSetActiveAsync(int id, bool active, CancellationToken cancellationToken)
    {
        var result = await _admin.SetActiveAsync(id, active, User.AcmsUserName(), cancellationToken);
        StatusMessage = result.Succeeded
            ? $"'{result.Server!.Name}' is now {(active ? "active" : "inactive")}."
            : string.Join(" ", result.Errors);

        return RedirectToPage();
    }
}

using System.ComponentModel.DataAnnotations;
using Acms.Core.Abstractions;
using Acms.Core.Domain;
using Acms.Core.Services;
using Acms.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Acms.Web.Pages.Admin.Servers;

/// <summary>Add a new AWACS server connection (no id) or change an existing one.</summary>
public class EditModel : PageModel
{
    private readonly IServerRepository _servers;
    private readonly ServerAdminService _admin;
    private readonly IAwacsClient _awacs;

    public EditModel(IServerRepository servers, ServerAdminService admin, IAwacsClient awacs)
    {
        _servers = servers;
        _admin = admin;
        _awacs = awacs;
    }

    [BindProperty(SupportsGet = true)]
    public int? Id { get; set; }

    [BindProperty]
    public ServerForm Input { get; set; } = new();

    public string? TestResult { get; private set; }
    public bool TestSucceeded { get; private set; }

    public bool IsNew => Id is null;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (IsNew)
        {
            return Page();
        }

        var server = await _servers.GetAsync(Id!.Value, cancellationToken);
        if (server is null)
        {
            return NotFound();
        }

        Input = new ServerForm { Name = server.Name, BaseUrl = server.BaseUrl, Description = server.Description };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var input = new ServerInput(Input.Name, Input.BaseUrl, Input.Description);
        var result = IsNew
            ? await _admin.CreateAsync(input, User.AcmsUserName(), cancellationToken)
            : await _admin.UpdateAsync(Id!.Value, input, User.AcmsUserName(), cancellationToken);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return Page();
        }

        TempData["StatusMessage"] = $"Saved '{result.Server!.Name}'.";
        return RedirectToPage("Index");
    }

    /// <summary>Calls wsdata.xml?ws=* on the entered URL without saving anything.</summary>
    public async Task<IActionResult> OnPostTestAsync(CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(Input.BaseUrl, UriKind.Absolute, out _))
        {
            TestResult = "Enter a valid base URL first.";
            return Page();
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));

        try
        {
            var probe = new AwacsServer { Id = Id ?? 0, Name = Input.Name, BaseUrl = Input.BaseUrl, IsActive = true };
            var workstations = await _awacs.GetWorkstationsAsync(probe, null, timeout.Token);
            TestSucceeded = true;
            TestResult = $"Connected. AWACS returned {workstations.Count} workstation(s).";
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            TestResult = ex is OperationCanceledException ? "No answer within 15 seconds." : ex.Message;
        }

        return Page();
    }

    public sealed class ServerForm
    {
        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(400), Display(Name = "Base URL")]
        public string BaseUrl { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }
    }
}

using Acms.Core.Services;
using Acms.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Acms.Web.Pages.Equipment;

/// <summary>
/// Sets one attribute (for example SPEED_SPEC) on many machines from a pasted list.
/// Requires the Engineer or Administrator role (see Program.cs).
/// </summary>
public class BulkModel : PageModel
{
    private readonly BulkUpdateService _bulk;

    public BulkModel(BulkUpdateService bulk, IOptions<EquipmentRulesOptions> rules)
    {
        _bulk = bulk;
        KnownAttributes = rules.Value.KnownAttributes;
    }

    [BindProperty]
    public string Attribute { get; set; } = "SPEED_SPEC";

    [BindProperty]
    public string? Input { get; set; }

    /// <summary>The rows to change, carried from the preview to the apply step.</summary>
    [BindProperty]
    public List<BulkItemInput> Items { get; set; } = [];

    public IReadOnlyList<string> KnownAttributes { get; }
    public BulkPreview? Preview { get; private set; }
    public IReadOnlyList<BulkApplyResult>? Results { get; private set; }
    public string? Info { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostPreviewAsync(CancellationToken cancellationToken)
    {
        Preview = await _bulk.PreviewAsync(Attribute, BulkInputParser.Parse(Input), cancellationToken);
        Attribute = Preview.Attribute;
        Items = Preview.Rows
            .Where(r => r.Status == BulkRowStatus.Change)
            .Select(r => new BulkItemInput
            {
                Selected = true,
                ServerId = r.ServerId!.Value,
                WsId = r.WsId,
                Current = r.CurrentValue,
                NewValue = r.NewValue!,
            })
            .ToList();

        ModelState.Clear();
        return Page();
    }

    public async Task<IActionResult> OnPostApplyAsync(CancellationToken cancellationToken)
    {
        var items = Items
            .Where(i => i.Selected)
            .Select(i => new BulkApplyItem(i.ServerId, i.WsId, i.Current, i.NewValue ?? string.Empty))
            .ToList();

        if (items.Count == 0)
        {
            Info = "No machine was selected, so nothing was changed.";
            return Page();
        }

        Results = await _bulk.ApplyAsync(Attribute.Trim(), items, User.AcmsUserName(), cancellationToken);
        return Page();
    }

    public sealed class BulkItemInput
    {
        public bool Selected { get; set; }
        public int ServerId { get; set; }
        public string WsId { get; set; } = string.Empty;
        public string? Current { get; set; }
        public string? NewValue { get; set; }
    }
}

using System.Text.Json;
using Acms.Core.Abstractions;
using Acms.Core.Domain;
using Acms.Core.Import;
using Acms.Core.Services;
using Acms.Infrastructure.Import;
using Acms.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Acms.Web.Pages.Equipment;

/// <summary>
/// Imports an update list (Excel, CSV, Word, PDF, e-mail or pasted table), shows what will change on
/// each machine and applies it after confirmation. Requires Engineer or Administrator (see Program.cs).
/// </summary>
public class BulkModel : PageModel
{
    private readonly BulkUpdateService _bulk;
    private readonly ImportOptions _import;
    private readonly IServerRepository _servers;

    public BulkModel(BulkUpdateService bulk, IOptions<EquipmentRulesOptions> rules, IOptions<ImportOptions> import, IServerRepository servers)
    {
        _bulk = bulk;
        _servers = servers;
        _import = import.Value;
        KnownAttributes = rules.Value.KnownAttributes;
    }

    [BindProperty]
    public IFormFile? Upload { get; set; }

    [BindProperty]
    public string? Pasted { get; set; }

    /// <summary>The table read from the file, carried between the steps.</summary>
    [BindProperty]
    public string? TableJson { get; set; }

    /// <summary>Target per column: WSID, an attribute name, or empty (ignored).</summary>
    [BindProperty]
    public List<string?> Mapping { get; set; } = [];

    /// <summary>Server on which machines that are not found are added; null = report them as not found.</summary>
    [BindProperty]
    public int? CreateOnServerId { get; set; }

    public IReadOnlyList<AwacsServer> Servers { get; private set; } = [];

    /// <summary>The machines to change, carried from the preview to the apply step.</summary>
    [BindProperty]
    public List<BulkItemInput> Items { get; set; } = [];

    public IReadOnlyList<string> KnownAttributes { get; }
    public IReadOnlyList<string> SupportedExtensions => TableFileReader.SupportedExtensions;
    public ImportTable? Table { get; private set; }
    public IReadOnlyList<string> MappingErrors { get; private set; } = [];
    public BulkPreview? Preview { get; private set; }
    public IReadOnlyList<BulkApplyResult>? Results { get; private set; }
    public string? Error { get; private set; }
    public string? Info { get; private set; }

    public void OnGet()
    {
    }

    /// <summary>CSV template (opens in Excel) with Ws and the usual workstation attributes as columns.</summary>
    public IActionResult OnGetTemplate()
    {
        var header = string.Join(",", new[] { "Ws" }.Concat(_import.TemplateColumns));
        var example = string.Join(",", new[] { "DB-QE1-001S" }.Concat(_import.TemplateColumns.Select(_ => "")));
        return File(System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(header + "\r\n" + example + "\r\n")).ToArray(),
            "text/csv", "acms-update-template.csv");
    }

    /// <summary>Step 1: read the uploaded file or pasted text, suggest the column mapping and preview.</summary>
    public async Task<IActionResult> OnPostReadAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (Upload is { Length: > 0 })
            {
                if (Upload.Length > _import.MaxFileSizeMb * 1024L * 1024L)
                {
                    Error = $"The file is larger than {_import.MaxFileSizeMb} MB.";
                    return Page();
                }

                await using var stream = Upload.OpenReadStream();
                Table = TableFileReader.Read(stream, Upload.FileName);
            }
            else if (!string.IsNullOrWhiteSpace(Pasted))
            {
                Table = ImportTable.Best("Pasted text", [ImportText.SplitPasted(Pasted)]);
            }
            else
            {
                Error = "Choose a file or paste a table first.";
                return Page();
            }
        }
        catch (ImportException ex)
        {
            Error = ex.Message;
            return Page();
        }

        if (Table.Rows.Count == 0)
        {
            Error = $"No table with machines was found in {Table.Source}.";
            Table = null;
            return Page();
        }

        Mapping = ColumnMapper.Suggest(Table, _import, KnownAttributes).Cast<string?>().ToList();
        TableJson = JsonSerializer.Serialize(Table);
        ModelState.Clear();
        await BuildPreviewAsync(cancellationToken);
        return Page();
    }

    /// <summary>Step 2: preview again after the column mapping was changed.</summary>
    public async Task<IActionResult> OnPostPreviewAsync(CancellationToken cancellationToken)
    {
        if (!LoadTable())
        {
            return Page();
        }

        ModelState.Clear();
        await BuildPreviewAsync(cancellationToken);
        return Page();
    }

    /// <summary>Step 3: apply the selected machines.</summary>
    public async Task<IActionResult> OnPostApplyAsync(CancellationToken cancellationToken)
    {
        LoadTable();

        var items = Items
            .Where(i => i.Selected && i.Values.Count > 0)
            .Select(i => new BulkApplyItem(
                i.ServerId,
                i.WsId,
                i.Values.ToDictionary(v => v.Attribute, v => v.New ?? string.Empty, StringComparer.OrdinalIgnoreCase),
                i.Values.ToDictionary(v => v.Attribute, v => v.Current ?? string.Empty, StringComparer.OrdinalIgnoreCase),
                i.Create))
            .ToList();

        if (items.Count == 0)
        {
            Info = "No machine was selected, so nothing was changed.";
            return Page();
        }

        Results = await _bulk.ApplyAsync(items, User.AcmsUserName(), cancellationToken);
        return Page();
    }

    public IEnumerable<string> Samples(int column) =>
        Table!.Rows.Select(r => r[column]).Where(v => v.Length > 0).Take(3);

    private bool LoadTable()
    {
        try
        {
            Table = string.IsNullOrEmpty(TableJson) ? null : JsonSerializer.Deserialize<ImportTable>(TableJson);
        }
        catch (JsonException)
        {
            Table = null;
        }

        if (Table is null)
        {
            Error = "The imported table was lost. Read the file again.";
        }

        return Table is not null;
    }

    private async Task BuildPreviewAsync(CancellationToken cancellationToken)
    {
        Servers = await _servers.GetAllAsync(includeInactive: false, cancellationToken);

        var (rows, errors) = ColumnMapper.ToRows(Table!, Mapping);
        if (errors.Count > 0)
        {
            MappingErrors = errors;
            return;
        }

        Preview = await _bulk.PreviewAsync(rows, cancellationToken, CreateOnServerId);
        Items = Preview.Rows
            .Where(r => r.Status is BulkRowStatus.Change or BulkRowStatus.Create)
            .Select(r => new BulkItemInput
            {
                Selected = true,
                Create = r.Status == BulkRowStatus.Create,
                ServerId = r.ServerId!.Value,
                WsId = r.WsId,
                Values = r.Values
                    .Where(v => v.Changed)
                    .Select(v => new BulkValueInput { Attribute = v.Attribute, Current = v.Current, New = v.New })
                    .ToList(),
            })
            .ToList();
    }

    public sealed class BulkItemInput
    {
        public bool Selected { get; set; }
        public bool Create { get; set; }
        public int ServerId { get; set; }
        public string WsId { get; set; } = string.Empty;
        public List<BulkValueInput> Values { get; set; } = [];
    }

    public sealed class BulkValueInput
    {
        public string Attribute { get; set; } = string.Empty;
        public string? Current { get; set; }
        public string? New { get; set; }
    }
}

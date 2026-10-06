using Acms.Core.Import;
using Acms.Core.Security;
using Acms.Core.Services;
using Acms.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Acms.Web.Api;

/// <summary>
/// Bulk update for scripts: the same preview and apply steps as the Import updates page.
/// <c>dryRun</c> defaults to true, so a request only changes AWACS when it says <c>"dryRun": false</c>.
/// </summary>
[ApiController]
[Route("api/bulk-update")]
[Authorize(Policy = AcmsPolicies.CanEditEquipment)]
public sealed class BulkUpdateController : ControllerBase
{
    private readonly BulkUpdateService _bulk;

    public BulkUpdateController(BulkUpdateService bulk)
    {
        _bulk = bulk;
    }

    /// <summary>
    /// Returns 200 with the preview (and the results when <c>dryRun</c> is false),
    /// or 400 when the list itself is invalid.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<BulkUpdateResponse>> Post([FromBody] BulkUpdateRequest body, CancellationToken cancellationToken)
    {
        var rows = (body.Rows ?? [])
            .Select((row, i) => new BulkInputRow(
                i + 1,
                (row.WsId ?? string.Empty).Trim(),
                (row.Values ?? []).ToDictionary(v => v.Key.Trim(), v => ImportText.NormalizeValue(v.Value), StringComparer.OrdinalIgnoreCase)))
            .ToList();

        var preview = await _bulk.PreviewAsync(rows, cancellationToken);
        if (!preview.IsValid)
        {
            return BadRequest(new BulkUpdateResponse(preview, null));
        }

        if (body.DryRun)
        {
            return Ok(new BulkUpdateResponse(preview, null));
        }

        var items = preview.Rows
            .Where(r => r.Status == BulkRowStatus.Change)
            .Select(r => new BulkApplyItem(
                r.ServerId!.Value,
                r.WsId,
                r.Values.Where(v => v.Changed).ToDictionary(v => v.Attribute, v => v.New, StringComparer.OrdinalIgnoreCase),
                r.Values.Where(v => v.Changed).ToDictionary(v => v.Attribute, v => v.Current ?? string.Empty, StringComparer.OrdinalIgnoreCase)))
            .ToList();

        var results = await _bulk.ApplyAsync(items, User.AcmsUserName(), cancellationToken);
        return Ok(new BulkUpdateResponse(preview, results));
    }
}

public sealed class BulkUpdateRequest
{
    /// <summary>One entry per machine: <c>{ "wsId": "DB-AXF-012S", "values": { "SPEED_SPEC": "28000" } }</c>.</summary>
    public List<BulkRowRequest>? Rows { get; init; }

    /// <summary>True (the default) only previews; false applies the changes.</summary>
    public bool DryRun { get; init; } = true;
}

public sealed record BulkRowRequest(string? WsId, Dictionary<string, string?>? Values);

public sealed record BulkUpdateResponse(BulkPreview Preview, IReadOnlyList<BulkApplyResult>? Results);

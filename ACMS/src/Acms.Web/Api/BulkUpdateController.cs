using Acms.Core.Security;
using Acms.Core.Services;
using Acms.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Acms.Web.Api;

/// <summary>
/// Bulk update for scripts: the same preview and apply steps as the Bulk update page.
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
    /// or 400 when the attribute or the list itself is invalid.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<BulkUpdateResponse>> Post([FromBody] BulkUpdateRequest body, CancellationToken cancellationToken)
    {
        var lines = (body.Rows ?? [])
            .Select((row, i) => new BulkInputLine(i + 1, (row.WsId ?? string.Empty).Trim(), BulkInputParser.NormalizeValue(row.Value)))
            .ToList();

        var preview = await _bulk.PreviewAsync(body.Attribute, lines, cancellationToken);
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
            .Select(r => new BulkApplyItem(r.ServerId!.Value, r.WsId, r.CurrentValue, r.NewValue!))
            .ToList();

        var results = await _bulk.ApplyAsync(preview.Attribute, items, User.AcmsUserName(), cancellationToken);
        return Ok(new BulkUpdateResponse(preview, results));
    }
}

public sealed class BulkUpdateRequest
{
    /// <summary>Attribute to set, e.g. SPEED_SPEC.</summary>
    public string? Attribute { get; init; }

    public List<BulkRowRequest>? Rows { get; init; }

    /// <summary>True (the default) only previews; false applies the changes.</summary>
    public bool DryRun { get; init; } = true;
}

public sealed record BulkRowRequest(string? WsId, string? Value);

public sealed record BulkUpdateResponse(BulkPreview Preview, IReadOnlyList<BulkApplyResult>? Results);

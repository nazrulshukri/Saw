using Acms.Core.Abstractions;
using Acms.Core.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Acms.Core.Services;

public enum BulkRowStatus
{
    /// <summary>Found on exactly one server and the value differs: will be changed.</summary>
    Change,

    /// <summary>Already has the requested value.</summary>
    NoChange,

    /// <summary>No new value on the line (empty or N/A).</summary>
    NoValue,

    /// <summary>The WSID or value breaks the edit rules.</summary>
    Invalid,

    /// <summary>The WSID is listed more than once; only the first line is used.</summary>
    Duplicate,

    /// <summary>No active AWACS server knows the WSID.</summary>
    NotFound,

    /// <summary>More than one AWACS server knows the WSID.</summary>
    Ambiguous,

    /// <summary>Not found, but at least one server could not be read.</summary>
    Unknown,
}

public sealed record BulkPreviewRow(int LineNumber, string WsId, string? NewValue, BulkRowStatus Status, string Message)
{
    public int? ServerId { get; init; }
    public string? ServerName { get; init; }
    public string? CurrentValue { get; init; }
}

/// <param name="Errors">Problems with the request itself; when present nothing was looked up.</param>
/// <param name="ServerErrors">Servers that could not be read.</param>
public sealed record BulkPreview(
    string Attribute,
    IReadOnlyList<BulkPreviewRow> Rows,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> ServerErrors)
{
    public bool IsValid => Errors.Count == 0;

    public int Count(BulkRowStatus status) => Rows.Count(r => r.Status == status);
}

/// <param name="ExpectedCurrent">Value shown in the preview. The change is refused if AWACS has something else now.</param>
public sealed record BulkApplyItem(int ServerId, string WsId, string? ExpectedCurrent, string NewValue);

public sealed record BulkApplyResult(BulkApplyItem Item, string? ServerName, EquipmentChangeResult Result);

/// <summary>
/// Sets one attribute (for example SPEED_SPEC) on many workstations: the list is checked and every
/// machine is located on the active AWACS servers first (preview), then each change goes through
/// <see cref="EquipmentChangeService"/>, so it is validated, verified and audited like a single edit.
/// </summary>
public sealed class BulkUpdateService
{
    public const int MaxLines = 500;

    private readonly IServerRepository _servers;
    private readonly IAwacsClient _awacs;
    private readonly EquipmentChangeService _changes;
    private readonly EditRules _rules;
    private readonly ILogger<BulkUpdateService> _logger;

    public BulkUpdateService(
        IServerRepository servers,
        IAwacsClient awacs,
        EquipmentChangeService changes,
        IOptions<EquipmentRulesOptions> rules,
        ILogger<BulkUpdateService> logger)
    {
        _servers = servers;
        _awacs = awacs;
        _changes = changes;
        _rules = new EditRules(rules.Value);
        _logger = logger;
    }

    /// <summary>Checks the list and reads the current values. Nothing is changed on AWACS.</summary>
    public async Task<BulkPreview> PreviewAsync(
        string? attribute,
        IReadOnlyList<BulkInputLine> lines,
        CancellationToken cancellationToken = default)
    {
        attribute = (attribute ?? string.Empty).Trim();

        var errors = new List<string>();
        if (!EditRules.IsValidIdentifier(attribute))
        {
            errors.Add("Enter the attribute to change, for example SPEED_SPEC.");
        }
        else if (_rules.IsReadOnly(attribute))
        {
            errors.Add($"Attribute '{attribute}' is read-only in ACMS.");
        }

        if (lines.Count == 0)
        {
            errors.Add("Paste at least one line with a WSID and its new value.");
        }
        else if (lines.Count > MaxLines)
        {
            errors.Add($"At most {MaxLines} lines can be processed at once; this list has {lines.Count}.");
        }

        if (errors.Count > 0)
        {
            return new BulkPreview(attribute, [], errors, []);
        }

        // 1. Check each line on its own.
        var rows = new BulkPreviewRow?[lines.Count];
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lookup = new List<string>();

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            if (!EditRules.IsValidIdentifier(line.WsId))
            {
                rows[i] = Row(line, BulkRowStatus.Invalid, "Not a valid WSID.");
            }
            else if (!seen.Add(line.WsId))
            {
                rows[i] = Row(line, BulkRowStatus.Duplicate, "Listed more than once; only the first line is used.");
            }
            else if (line.Value is null)
            {
                rows[i] = Row(line, BulkRowStatus.NoValue, "No new value (empty or N/A); skipped.");
            }
            else if (_rules.Validate(attribute, line.Value) is { Count: > 0 } problems)
            {
                rows[i] = Row(line, BulkRowStatus.Invalid, string.Join(" ", problems));
            }
            else
            {
                lookup.Add(line.WsId);
            }
        }

        // 2. Find the machines on every active server (one wsdata.xml?ws=A,B,C read per server).
        var serverErrors = new List<string>();
        var found = new Dictionary<string, List<(AwacsServer Server, Workstation Workstation)>>(StringComparer.OrdinalIgnoreCase);

        if (lookup.Count > 0)
        {
            var servers = await _servers.GetAllAsync(includeInactive: false, cancellationToken);
            var reads = await Task.WhenAll(servers.Select(server => ReadAsync(server, lookup, cancellationToken)));

            foreach (var (server, workstations, error) in reads)
            {
                if (error is not null)
                {
                    serverErrors.Add($"Could not read '{server.Name}': {error}");
                    continue;
                }

                foreach (var workstation in workstations)
                {
                    if (!found.TryGetValue(workstation.WsId, out var list))
                    {
                        found[workstation.WsId] = list = [];
                    }

                    list.Add((server, workstation));
                }
            }
        }

        // 3. Decide what happens to each machine.
        for (var i = 0; i < lines.Count; i++)
        {
            if (rows[i] is not null)
            {
                continue;
            }

            var line = lines[i];

            if (!found.TryGetValue(line.WsId, out var matches))
            {
                rows[i] = serverErrors.Count > 0
                    ? Row(line, BulkRowStatus.Unknown, "Not found on the servers that answered; some servers could not be read.")
                    : Row(line, BulkRowStatus.NotFound, "Not found on any active AWACS server.");
                continue;
            }

            if (matches.Count > 1)
            {
                rows[i] = Row(line, BulkRowStatus.Ambiguous,
                    "Found on more than one server: " + string.Join(", ", matches.Select(m => m.Server.Name))
                    + ". Change it from its equipment page instead.");
                continue;
            }

            var (server, workstation) = matches[0];
            var current = workstation.Get(attribute);
            var located = (BulkRowStatus status, string message) => Row(line, status, message) with
            {
                ServerId = server.Id,
                ServerName = server.Name,
                CurrentValue = current,
            };

            if (!_rules.IsSettable(attribute, workstation))
            {
                rows[i] = located(BulkRowStatus.Invalid, $"'{attribute}' does not exist on this workstation.");
            }
            else if (AttributeDiff.Changes(workstation.Attributes, new Dictionary<string, string> { [attribute] = line.Value! }).Count == 0)
            {
                rows[i] = located(BulkRowStatus.NoChange, "Already set.");
            }
            else
            {
                rows[i] = located(BulkRowStatus.Change, string.Empty);
            }
        }

        return new BulkPreview(attribute, rows.Select(r => r!).ToList(), [], serverErrors);
    }

    /// <summary>
    /// Applies the changes one machine at a time. Each one is validated again, compared with the value
    /// shown in the preview, re-read, verified and written to the audit trail.
    /// </summary>
    public async Task<IReadOnlyList<BulkApplyResult>> ApplyAsync(
        string attribute,
        IReadOnlyList<BulkApplyItem> items,
        string userName,
        CancellationToken cancellationToken = default)
    {
        if (items.Count > MaxLines)
        {
            throw new ArgumentException($"At most {MaxLines} changes can be applied at once.", nameof(items));
        }

        var serverNames = (await _servers.GetAllAsync(includeInactive: true, cancellationToken))
            .ToDictionary(s => s.Id, s => s.Name);

        var results = new List<BulkApplyResult>(items.Count);

        foreach (var item in items)
        {
            var request = new EquipmentEditRequest(
                item.ServerId,
                item.WsId,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { [attribute] = item.NewValue },
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { [attribute] = item.ExpectedCurrent ?? string.Empty });

            var result = await _changes.EditAsync(request, userName, cancellationToken);
            results.Add(new BulkApplyResult(item, serverNames.GetValueOrDefault(item.ServerId), result));
        }

        _logger.LogInformation("Bulk update of {Attribute} by {User}: {Succeeded} of {Total} succeeded",
            attribute, userName, results.Count(r => r.Result.Succeeded), results.Count);

        return results;
    }

    private async Task<(AwacsServer Server, IReadOnlyList<Workstation> Workstations, string? Error)> ReadAsync(
        AwacsServer server,
        IReadOnlyCollection<string> wsIds,
        CancellationToken cancellationToken)
    {
        try
        {
            return (server, await _awacs.GetWorkstationsAsync(server, wsIds, cancellationToken), null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Bulk preview could not read AWACS server {Server}", server.Name);
            return (server, [], ex.Message);
        }
    }

    private static BulkPreviewRow Row(BulkInputLine line, BulkRowStatus status, string message) =>
        new(line.LineNumber, line.WsId, line.Value, status, message);
}

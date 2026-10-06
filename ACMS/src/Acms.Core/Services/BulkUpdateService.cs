using Acms.Core.Abstractions;
using Acms.Core.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Acms.Core.Services;

public enum BulkRowStatus
{
    /// <summary>Found on exactly one server and the value differs: will be changed.</summary>
    Change,

    /// <summary>Not found anywhere: will be added on the chosen server.</summary>
    Create,

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

/// <summary>One machine from the imported list; a null value means "no new value" for that attribute.</summary>
public sealed record BulkInputRow(int RowNumber, string WsId, IReadOnlyDictionary<string, string?> Values);

/// <param name="Current">Value on AWACS (null when the machine was not found or the attribute is empty).</param>
/// <param name="Changed">True when <paramref name="New"/> differs from <paramref name="Current"/>.</param>
public sealed record BulkValueChange(string Attribute, string? Current, string New, bool Changed);

public sealed record BulkPreviewRow(int RowNumber, string WsId, BulkRowStatus Status, string Message)
{
    public int? ServerId { get; init; }
    public string? ServerName { get; init; }

    /// <summary>The requested values with the current AWACS value next to each.</summary>
    public IReadOnlyList<BulkValueChange> Values { get; init; } = [];
}

/// <param name="Attributes">Attributes in the list.</param>
/// <param name="Errors">Problems with the request itself; when present nothing was looked up.</param>
/// <param name="ServerErrors">Servers that could not be read.</param>
public sealed record BulkPreview(
    IReadOnlyList<string> Attributes,
    IReadOnlyList<BulkPreviewRow> Rows,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> ServerErrors)
{
    public bool IsValid => Errors.Count == 0;

    public int Count(BulkRowStatus status) => Rows.Count(r => r.Status == status);
}

/// <param name="NewValues">Attributes to set.</param>
/// <param name="ExpectedCurrent">Values shown in the preview. A value AWACS has changed since is refused.</param>
/// <param name="Create">True to add the workstation instead of changing it.</param>
public sealed record BulkApplyItem(
    int ServerId,
    string WsId,
    IReadOnlyDictionary<string, string> NewValues,
    IReadOnlyDictionary<string, string> ExpectedCurrent,
    bool Create = false);

public sealed record BulkApplyResult(BulkApplyItem Item, string? ServerName, EquipmentChangeResult Result);

/// <summary>
/// Sets attributes (for example SPEED_SPEC) on many workstations from an imported list: the list is
/// checked and every machine is located on the active AWACS servers first (preview), then each machine
/// goes through <see cref="EquipmentChangeService"/>, so it is validated, verified and audited like a
/// single edit.
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
    /// <param name="createOnServerId">
    /// Server on which machines that are not found anywhere are added; null reports them as not found.
    /// </param>
    public async Task<BulkPreview> PreviewAsync(
        IReadOnlyList<BulkInputRow> lines,
        CancellationToken cancellationToken = default,
        int? createOnServerId = null)
    {
        var attributes = lines
            .SelectMany(l => l.Values.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var errors = new List<string>();
        foreach (var attribute in attributes)
        {
            if (!EditRules.IsValidIdentifier(attribute))
            {
                errors.Add($"'{attribute}' is not a valid attribute name.");
            }
            else if (_rules.IsReadOnly(attribute))
            {
                errors.Add($"Attribute '{attribute}' is read-only in ACMS.");
            }
        }

        if (lines.Count == 0)
        {
            errors.Add("The list has no machines.");
        }
        else if (lines.Count > MaxLines)
        {
            errors.Add($"At most {MaxLines} machines can be processed at once; this list has {lines.Count}.");
        }

        if (errors.Count > 0)
        {
            return new BulkPreview(attributes, [], errors, []);
        }

        // 1. Check each line on its own.
        var rows = new BulkPreviewRow?[lines.Count];
        var requested = new Dictionary<string, string>[lines.Count];
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lookup = new List<string>();

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            requested[i] = line.Values
                .Where(v => v.Value is not null)
                .ToDictionary(v => v.Key, v => v.Value!, StringComparer.OrdinalIgnoreCase);

            var problems = requested[i].SelectMany(v => _rules.Validate(v.Key, v.Value)).ToList();

            if (!EditRules.IsValidIdentifier(line.WsId))
            {
                rows[i] = Row(line, requested[i], BulkRowStatus.Invalid, "Not a valid WSID.");
            }
            else if (!seen.Add(line.WsId))
            {
                rows[i] = Row(line, requested[i], BulkRowStatus.Duplicate, "Listed more than once; only the first row is used.");
            }
            else if (requested[i].Count == 0)
            {
                rows[i] = Row(line, requested[i], BulkRowStatus.NoValue, "No new value (empty or N/A); skipped.");
            }
            else if (problems.Count > 0)
            {
                rows[i] = Row(line, requested[i], BulkRowStatus.Invalid, string.Join(" ", problems));
            }
            else
            {
                lookup.Add(line.WsId);
            }
        }

        // 2. Find the machines on every active server (one wsdata.xml?ws=A,B,C read per server).
        var serverErrors = new List<string>();
        AwacsServer? createServer = null;
        var found = new Dictionary<string, List<(AwacsServer Server, Workstation Workstation)>>(StringComparer.OrdinalIgnoreCase);

        if (lookup.Count > 0)
        {
            var servers = await _servers.GetAllAsync(includeInactive: false, cancellationToken);
            createServer = servers.FirstOrDefault(s => s.Id == createOnServerId);
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
            var values = requested[i];

            if (!found.TryGetValue(line.WsId, out var matches))
            {
                var notKnown = values.Keys.Where(name => !_rules.IsSettable(name, new Workstation(line.WsId))).ToList();
                if (createServer is not null && serverErrors.Count == 0 && notKnown.Count == 0)
                {
                    rows[i] = new BulkPreviewRow(line.RowNumber, line.WsId, BulkRowStatus.Create, "New machine.")
                    {
                        ServerId = createServer.Id,
                        ServerName = createServer.Name,
                        Values = values.Select(v => new BulkValueChange(v.Key, null, v.Value, true)).ToList(),
                    };
                    continue;
                }

                rows[i] = serverErrors.Count > 0
                    ? Row(line, values, BulkRowStatus.Unknown, "Not found on the servers that answered; some servers could not be read.")
                    : Row(line, values, BulkRowStatus.NotFound, "Not found on any active AWACS server.");
                continue;
            }

            if (matches.Count > 1)
            {
                rows[i] = Row(line, values, BulkRowStatus.Ambiguous,
                    "Found on more than one server: " + string.Join(", ", matches.Select(m => m.Server.Name))
                    + ". Change it from its equipment page instead.");
                continue;
            }

            var (server, workstation) = matches[0];
            var changes = AttributeDiff.Changes(workstation.Attributes, values);
            var unknown = values.Keys.Where(name => !_rules.IsSettable(name, workstation)).ToList();

            var (status, message) = unknown.Count > 0
                ? (BulkRowStatus.Invalid, $"Not on this workstation: {string.Join(", ", unknown)}.")
                : changes.Count > 0
                    ? (BulkRowStatus.Change, string.Empty)
                    : (BulkRowStatus.NoChange, "Already set.");

            rows[i] = new BulkPreviewRow(line.RowNumber, line.WsId, status, message)
            {
                ServerId = server.Id,
                ServerName = server.Name,
                Values = values
                    .Select(v => new BulkValueChange(v.Key, workstation.Get(v.Key), v.Value, changes.ContainsKey(v.Key)))
                    .ToList(),
            };
        }

        return new BulkPreview(attributes, rows.Select(r => r!).ToList(), [], serverErrors);
    }

    /// <summary>
    /// Applies the changes one machine at a time (one AWACS call per machine). Each one is validated again,
    /// compared with the values shown in the preview, re-read, verified and written to the audit trail.
    /// </summary>
    public async Task<IReadOnlyList<BulkApplyResult>> ApplyAsync(
        IReadOnlyList<BulkApplyItem> items,
        string userName,
        CancellationToken cancellationToken = default)
    {
        if (items.Count > MaxLines)
        {
            throw new ArgumentException($"At most {MaxLines} machines can be changed at once.", nameof(items));
        }

        var serverNames = (await _servers.GetAllAsync(includeInactive: true, cancellationToken))
            .ToDictionary(s => s.Id, s => s.Name);

        var results = new List<BulkApplyResult>(items.Count);

        foreach (var item in items)
        {
            var result = item.Create
                ? await _changes.CreateAsync(new EquipmentCreateRequest(item.ServerId, item.WsId, item.NewValues), userName, cancellationToken)
                : await _changes.EditAsync(new EquipmentEditRequest(item.ServerId, item.WsId, item.NewValues, item.ExpectedCurrent), userName, cancellationToken);
            results.Add(new BulkApplyResult(item, serverNames.GetValueOrDefault(item.ServerId), result));
        }

        _logger.LogInformation("Bulk update by {User}: {Succeeded} of {Total} machines succeeded",
            userName, results.Count(r => r.Result.Succeeded), results.Count);

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

    private static BulkPreviewRow Row(
        BulkInputRow line,
        IReadOnlyDictionary<string, string> values,
        BulkRowStatus status,
        string message) =>
        new(line.RowNumber, line.WsId, status, message)
        {
            Values = values.Select(v => new BulkValueChange(v.Key, null, v.Value, false)).ToList(),
        };
}

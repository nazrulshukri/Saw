using System.Text.Json;
using Acms.Core.Abstractions;
using Acms.Core.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Acms.Core.Services;

/// <summary>A request to change attributes of one workstation on one AWACS server.</summary>
/// <param name="Values">Attribute name to requested value. Unchanged values are ignored.</param>
/// <param name="ExpectedOriginal">
/// Values the user saw when they opened the form. If AWACS now has something different for a
/// changed attribute, the request is rejected so one engineer cannot silently overwrite another.
/// </param>
public sealed record EquipmentEditRequest(
    int ServerId,
    string WsId,
    IReadOnlyDictionary<string, string> Values,
    IReadOnlyDictionary<string, string>? ExpectedOriginal = null);

public sealed class EquipmentChangeResult
{
    public required AuditOutcome Outcome { get; init; }
    public required string Message { get; init; }
    public required string CorrelationId { get; init; }

    /// <summary>True when the request was rejected because AWACS changed since the form was loaded.</summary>
    public bool IsConflict { get; init; }

    public IReadOnlyDictionary<string, string> Changes { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string?> Before { get; init; } = new Dictionary<string, string?>();
    public IReadOnlyDictionary<string, string?> After { get; init; } = new Dictionary<string, string?>();
    public IReadOnlyList<AttributeMismatch> Mismatches { get; init; } = [];

    /// <summary>False if the audit row could not be stored. The change itself is unaffected.</summary>
    public bool AuditWritten { get; set; }

    public bool Succeeded => Outcome is AuditOutcome.Success or AuditOutcome.NoChange;
}

/// <summary>
/// Change orchestration for equipment edits (slide "System Logic Diagram"):
/// validate, resolve the server, capture before-values, apply only the differences,
/// re-read and verify, then write the audit record. Nothing is stored in ACMS except the audit row.
/// </summary>
public sealed class EquipmentChangeService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private readonly IServerRepository _servers;
    private readonly IAwacsClient _awacs;
    private readonly IAuditLog _audit;
    private readonly EditRules _rules;
    private readonly TimeProvider _clock;
    private readonly ILogger<EquipmentChangeService> _logger;

    public EquipmentChangeService(
        IServerRepository servers,
        IAwacsClient awacs,
        IAuditLog audit,
        IOptions<EquipmentRulesOptions> rules,
        TimeProvider clock,
        ILogger<EquipmentChangeService> logger)
    {
        _servers = servers;
        _awacs = awacs;
        _audit = audit;
        _rules = new EditRules(rules.Value);
        _clock = clock;
        _logger = logger;
    }

    public async Task<EquipmentChangeResult> EditAsync(
        EquipmentEditRequest request,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString("N");

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["User"] = userName,
            ["ServerId"] = request.ServerId,
            ["WsId"] = request.WsId,
        });

        var audit = new AuditEntry
        {
            Action = AuditAction.EquipmentEdit,
            UserName = userName,
            ServerId = request.ServerId,
            WsId = request.WsId,
            RequestedJson = ToJson(request.Values),
            CorrelationId = correlationId,
        };

        // 1. Validate the request itself.
        var errors = ValidateRequest(request);
        if (errors.Count > 0)
        {
            return await RejectAsync(audit, string.Join(" ", errors), cancellationToken);
        }

        // 2. Resolve the target AWACS server.
        var server = await _servers.GetAsync(request.ServerId, cancellationToken);
        if (server is null)
        {
            return await RejectAsync(audit, $"AWACS server {request.ServerId} does not exist.", cancellationToken);
        }

        audit.ServerName = server.Name;

        if (!server.IsActive)
        {
            return await RejectAsync(audit, $"AWACS server '{server.Name}' is inactive.", cancellationToken);
        }

        // 3. Capture the before-state.
        Workstation? before;
        try
        {
            before = await _awacs.GetWorkstationAsync(server, request.WsId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Could not read workstation before the change");
            return await CompleteAsync(audit, new EquipmentChangeResult
            {
                Outcome = AuditOutcome.Failed,
                Message = $"Could not read the current values from '{server.Name}': {ex.Message}",
                CorrelationId = correlationId,
            }, CancellationToken.None);
        }

        if (before is null)
        {
            return await RejectAsync(
                audit, $"Workstation '{request.WsId}' was not found on '{server.Name}'.", cancellationToken);
        }

        errors = ValidateAgainstCurrent(request.Values, before);
        if (errors.Count > 0)
        {
            return await RejectAsync(audit, string.Join(" ", errors), cancellationToken);
        }

        var changes = AttributeDiff.Changes(before.Attributes, request.Values);
        if (changes.Count == 0)
        {
            return await CompleteAsync(audit, new EquipmentChangeResult
            {
                Outcome = AuditOutcome.NoChange,
                Message = "All requested values are already set. Nothing was sent to AWACS.",
                CorrelationId = correlationId,
            }, cancellationToken);
        }

        var beforeValues = AttributeDiff.Select(before.Attributes, changes.Keys);
        audit.BeforeJson = ToJson(beforeValues);

        var conflicts = FindConflicts(changes.Keys, request.ExpectedOriginal, before);
        if (conflicts.Count > 0)
        {
            return await CompleteAsync(audit, new EquipmentChangeResult
            {
                Outcome = AuditOutcome.Rejected,
                IsConflict = true,
                Message = "These attributes were changed on AWACS after you loaded the page: "
                    + string.Join(", ", conflicts) + ". Reload and try again.",
                CorrelationId = correlationId,
                Changes = changes,
                Before = beforeValues,
            }, cancellationToken);
        }

        // 4. Apply. From here on the change may have reached AWACS, so verification and the audit
        //    row must complete even if the caller disconnects.
        AwacsUpdateResult update;
        try
        {
            update = await _awacs.UpdateAttributesAsync(server, request.WsId, changes, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AWACS update call failed");
            update = new AwacsUpdateResult(false, ex.Message);
        }

        // Always re-read, so the audit shows what is really on AWACS even after a failure.
        Workstation? after = null;
        string? rereadError = null;
        try
        {
            after = await _awacs.GetWorkstationAsync(server, request.WsId, CancellationToken.None);
            if (after is null)
            {
                rereadError = "the workstation was not returned";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not re-read workstation after the change");
            rereadError = ex.Message;
        }

        var afterValues = after is null
            ? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            : AttributeDiff.Select(after.Attributes, changes.Keys);

        if (after is not null)
        {
            audit.AfterJson = ToJson(afterValues);
        }

        var mismatches = after is null
            ? changes.Select(c => new AttributeMismatch(c.Key, c.Value, null)).ToList()
            : AttributeDiff.Verify(after.Attributes, changes);

        var succeeded = update.Accepted && after is not null && mismatches.Count == 0;

        return await CompleteAsync(audit, new EquipmentChangeResult
        {
            Outcome = succeeded ? AuditOutcome.Success : AuditOutcome.Failed,
            Message = succeeded
                ? $"{changes.Count} attribute(s) updated on '{server.Name}' and verified."
                : BuildFailureMessage(update, rereadError, after is null ? [] : mismatches),
            CorrelationId = correlationId,
            Changes = changes,
            Before = beforeValues,
            After = afterValues,
            Mismatches = mismatches,
        }, CancellationToken.None);
    }

    private List<string> ValidateRequest(EquipmentEditRequest request)
    {
        var errors = new List<string>();

        if (!EditRules.IsValidIdentifier(request.WsId))
        {
            errors.Add("WSID is missing or contains invalid characters.");
        }

        if (request.Values is null || request.Values.Count == 0)
        {
            errors.Add("No attribute values were supplied.");
            return errors;
        }

        foreach (var (name, value) in request.Values)
        {
            errors.AddRange(_rules.Validate(name, value));
        }

        return errors;
    }

    private List<string> ValidateAgainstCurrent(IReadOnlyDictionary<string, string> values, Workstation current)
    {
        return values.Keys
            .Where(name => !_rules.IsSettable(name, current))
            .Select(name => $"Attribute '{name}' does not exist on workstation '{current.WsId}'.")
            .ToList();
    }

    private static List<string> FindConflicts(
        IEnumerable<string> changedNames,
        IReadOnlyDictionary<string, string>? expectedOriginal,
        Workstation current)
    {
        if (expectedOriginal is null)
        {
            return [];
        }

        var expected = new Dictionary<string, string>(expectedOriginal, StringComparer.OrdinalIgnoreCase);

        return changedNames
            .Where(name => expected.TryGetValue(name, out var seen)
                && (seen ?? string.Empty).Trim() != (current.Get(name) ?? string.Empty).Trim())
            .ToList();
    }

    private static string BuildFailureMessage(
        AwacsUpdateResult update,
        string? rereadError,
        IReadOnlyList<AttributeMismatch> mismatches)
    {
        var parts = new List<string>();

        if (!update.Accepted)
        {
            parts.Add($"AWACS did not accept the update: {update.Detail ?? "no detail"}.");
        }

        if (rereadError is not null)
        {
            parts.Add($"Could not re-read the workstation to verify: {rereadError}.");
        }

        if (mismatches.Count > 0)
        {
            parts.Add("Not applied: " + string.Join(", ", mismatches.Select(m =>
                $"{m.Name} (expected '{m.Expected}', found '{m.Actual ?? "missing"}')")) + ".");
        }

        parts.Add("Nothing was stored in ACMS except this audit record. Check the values and retry.");
        return string.Join(" ", parts);
    }

    private Task<EquipmentChangeResult> RejectAsync(AuditEntry audit, string message, CancellationToken cancellationToken)
    {
        return CompleteAsync(audit, new EquipmentChangeResult
        {
            Outcome = AuditOutcome.Rejected,
            Message = message,
            CorrelationId = audit.CorrelationId,
        }, cancellationToken);
    }

    private async Task<EquipmentChangeResult> CompleteAsync(
        AuditEntry audit,
        EquipmentChangeResult result,
        CancellationToken cancellationToken)
    {
        audit.TimestampUtc = _clock.GetUtcNow().UtcDateTime;
        audit.Outcome = result.Outcome;
        audit.Message = result.Message;

        try
        {
            await _audit.WriteAsync(audit, cancellationToken);
            result.AuditWritten = true;
        }
        catch (Exception ex)
        {
            // The AWACS change (if any) has already happened; losing the audit row must be visible.
            _logger.LogCritical(ex, "Audit record could not be written. Outcome {Outcome}: {Message}",
                result.Outcome, result.Message);
        }

        _logger.LogInformation("Equipment edit {Outcome}: {Message}", result.Outcome, result.Message);
        return result;
    }

    private static string ToJson<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);
}

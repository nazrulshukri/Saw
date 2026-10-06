namespace Acms.Core.Domain;

/// <summary>
/// One row of the central audit trail: who did what, when, on which server and
/// workstation, the values before and after, and whether it worked.
/// </summary>
public class AuditEntry
{
    public long Id { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string UserName { get; set; } = string.Empty;
    public AuditAction Action { get; set; }
    public AuditOutcome Outcome { get; set; }

    public int? ServerId { get; set; }
    public string? ServerName { get; set; }
    public string? WsId { get; set; }

    /// <summary>JSON object of the values the user asked for.</summary>
    public string? RequestedJson { get; set; }

    /// <summary>JSON object of the changed attributes as they were before the change.</summary>
    public string? BeforeJson { get; set; }

    /// <summary>JSON object of the changed attributes as re-read from AWACS after the change.</summary>
    public string? AfterJson { get; set; }

    public string? Message { get; set; }

    /// <summary>Ties the audit row to the application log lines for the same request.</summary>
    public string CorrelationId { get; set; } = string.Empty;
}

public enum AuditAction
{
    EquipmentEdit,
    EquipmentAdd,
    EquipmentDelete,
    ServerCreate,
    ServerUpdate,
    ServerActivate,
    ServerDeactivate,
}

public enum AuditOutcome
{
    /// <summary>The change was applied and verified.</summary>
    Success,

    /// <summary>The request asked for values that were already set. Nothing was sent to AWACS.</summary>
    NoChange,

    /// <summary>ACMS refused the request before contacting AWACS (validation, permission, conflict).</summary>
    Rejected,

    /// <summary>AWACS was contacted but the change could not be applied or verified.</summary>
    Failed,
}

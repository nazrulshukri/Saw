namespace Acms.Core.Domain;

/// <summary>
/// An AWACS server that ACMS manages. Stored in the ACMS configuration database.
/// </summary>
public class AwacsServer
{
    public int Id { get; set; }

    /// <summary>Display name, e.g. "AWACS ATSN Line 1". Must be unique.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Root URL of the AWACS server, e.g. "http://awacs01.company.local/".</summary>
    public string BaseUrl { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Inactive servers are hidden from the dashboard and cannot be changed.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedUtc { get; set; }
    public string? UpdatedBy { get; set; }
}

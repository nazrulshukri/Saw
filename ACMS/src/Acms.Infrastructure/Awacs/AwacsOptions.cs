namespace Acms.Infrastructure.Awacs;

/// <summary>
/// AWACS connector settings, bound from the "Awacs" section.
/// The paths and the update query format must match "AWACS API RESTful.pdf".
/// </summary>
public sealed class AwacsOptions
{
    public const string SectionName = "Awacs";

    /// <summary>Use the in-memory fake instead of real AWACS servers. Development only.</summary>
    public bool UseFake { get; set; }

    /// <summary>Relative path of the read interface. ACMS appends <c>?ws=WSID</c>, <c>?ws=*</c> or <c>?ws=WS1,WS2</c>.</summary>
    public string WorkstationDataPath { get; set; } = "template/wsdata.xml";

    /// <summary>Relative path of the update interface.</summary>
    public string UpdatePath { get; set; } = "template/wswoupdate.html";

    /// <summary>
    /// Query string sent to <see cref="UpdatePath"/> for each changed attribute.
    /// Tokens: <c>{ws}</c>, <c>{name}</c>, <c>{value}</c>. Each token is URL-encoded.
    /// CONFIRM against the AWACS API document before production use.
    /// </summary>
    public string UpdateQueryTemplate { get; set; } = "ws={ws}&setwsattr={name}&value={value}";

    /// <summary>
    /// Text that marks an update response as failed even with HTTP 200 (case-insensitive).
    /// Leave empty if AWACS uses HTTP status codes; verification by re-reading still applies.
    /// </summary>
    public string[] UpdateFailureMarkers { get; set; } = [];

    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Extra attempts for read calls on network errors and HTTP 5xx. Updates are never retried.</summary>
    public int ReadRetryCount { get; set; } = 2;

    /// <summary>Send the application pool / service account identity to AWACS (Windows auth).</summary>
    public bool UseDefaultCredentials { get; set; } = true;

    /// <summary>
    /// XML element names that represent one workstation in <c>wsdata.xml</c> (case-insensitive).
    /// If none are found, each repeated child of the root is treated as a workstation.
    /// </summary>
    public string[] WorkstationElementNames { get; set; } = ["ws", "workstation"];

    /// <summary>XML attribute or child element names that hold the workstation id (case-insensitive).</summary>
    public string[] WorkstationIdNames { get; set; } = ["WSID", "id", "name"];
}

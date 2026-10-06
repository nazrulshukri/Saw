namespace Acms.Infrastructure.Awacs;

/// <summary>
/// AWACS connector settings, bound from the "Awacs" section. Defaults follow the ITEC document
/// "Urls for manipulating workstations".
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
    /// How <c>setwsattr</c> is written: <see cref="AwacsAttributeFormat.Quoted"/>
    /// (<c>WsId="WSID",attr="value"</c>, values may contain commas) or <see cref="AwacsAttributeFormat.Colon"/>
    /// (<c>WsId:WSID,attr:value</c>). All changed attributes are sent in one call.
    /// </summary>
    public AwacsAttributeFormat UpdateAttributeFormat { get; set; } = AwacsAttributeFormat.Quoted;

    /// <summary>Largest number of WSIDs in one <c>wsdata.xml?ws=A,B,C</c> read; longer lists are split.</summary>
    public int MaxIdsPerRequest { get; set; } = 50;

    /// <summary>
    /// Text that marks an update response as failed even with HTTP 200 (case-insensitive).
    /// Leave empty if AWACS uses HTTP status codes; verification by re-reading still applies.
    /// </summary>
    public string[] UpdateFailureMarkers { get; set; } = [];

    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Extra attempts for read calls on network errors and HTTP 5xx. Updates are never retried.</summary>
    public int ReadRetryCount { get; set; } = 2;

    /// <summary>
    /// AWACS user that ACMS logs in with before changing or adding workstations. AWACS ignores changes
    /// from sessions that are not logged in. Leave empty to not log in. Keep the password out of
    /// source control: use user secrets or the environment variable Awacs__Password.
    /// </summary>
    public string? Username { get; set; }

    public string? Password { get; set; }

    /// <summary>AWACS login page; the user name and password are sent as Awacs_Username / Awacs_password.</summary>
    public string LoginPath { get; set; } = "template/general/home.html";

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

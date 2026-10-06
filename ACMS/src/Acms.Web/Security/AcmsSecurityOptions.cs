namespace Acms.Web.Security;

/// <summary>Bound from "Acms:Security".</summary>
public sealed class AcmsSecurityOptions
{
    public const string SectionName = "Acms:Security";

    /// <summary>
    /// ACMS role to the Active Directory groups that grant it, e.g.
    /// "Engineer": ["COMPANY\\ACMS-Engineers"]. Group SIDs (S-1-5-21-...) also work.
    /// </summary>
    public Dictionary<string, string[]> RoleMappings { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public DevAuthenticationOptions DevAuthentication { get; set; } = new();
}

/// <summary>
/// Fake sign-in for local development on machines without Windows authentication.
/// Ignored outside the Development environment.
/// </summary>
public sealed class DevAuthenticationOptions
{
    public bool Enabled { get; set; }
    public string UserName { get; set; } = "DEV\\developer";
    public string[] Roles { get; set; } = [];
}

namespace Acms.Core.Security;

/// <summary>ACMS roles. Active Directory groups are mapped to these in configuration.</summary>
public static class AcmsRoles
{
    /// <summary>Server and role configuration, plus everything an engineer can do.</summary>
    public const string Administrator = "Administrator";

    /// <summary>Equipment add / edit / delete.</summary>
    public const string Engineer = "Engineer";

    /// <summary>Read-only access and audit review.</summary>
    public const string Viewer = "Viewer";

    public static readonly string[] All = [Administrator, Engineer, Viewer];
}

public static class AcmsPolicies
{
    public const string CanView = nameof(CanView);
    public const string CanEditEquipment = nameof(CanEditEquipment);
    public const string CanAdminister = nameof(CanAdminister);
}

namespace Acms.Core.Services;

/// <summary>Field and format rules for equipment changes. Bound from the "EquipmentRules" section.</summary>
public sealed class EquipmentRulesOptions
{
    public const string SectionName = "EquipmentRules";

    /// <summary>Attributes that can never be changed from ACMS (case-insensitive).</summary>
    public string[] ReadOnlyAttributes { get; set; } = ["WSID"];

    public int MaxValueLength { get; set; } = 256;

    /// <summary>
    /// When false (the default), only attributes the workstation already has can be edited.
    /// This stops typos from creating new attributes on AWACS.
    /// </summary>
    public bool AllowNewAttributes { get; set; }
}

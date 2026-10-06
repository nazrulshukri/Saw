namespace Acms.Core.Import;

/// <summary>Settings for importing update lists. Bound from the "Import" section.</summary>
public sealed class ImportOptions
{
    public const string SectionName = "Import";

    /// <summary>
    /// Column titles that set an attribute although they are not named like it,
    /// e.g. "Change T Speed" (the IE UPH table) = SPEED_SPEC. Titles are compared case-insensitively.
    /// </summary>
    public Dictionary<string, string> ColumnAliases { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Largest file accepted on the import page.</summary>
    public int MaxFileSizeMb { get; set; } = 10;
}

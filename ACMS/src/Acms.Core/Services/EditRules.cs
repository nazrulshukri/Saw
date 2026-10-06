using System.Text.RegularExpressions;
using Acms.Core.Domain;

namespace Acms.Core.Services;

/// <summary>Field and format checks shared by single and bulk equipment edits.</summary>
public sealed partial class EditRules
{
    private readonly EquipmentRulesOptions _options;
    private readonly HashSet<string> _readOnly;
    private readonly HashSet<string> _known;

    public EditRules(EquipmentRulesOptions options)
    {
        _options = options;
        _readOnly = new HashSet<string>(options.ReadOnlyAttributes, StringComparer.OrdinalIgnoreCase);
        _known = new HashSet<string>(options.KnownAttributes, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Letters, digits, '-', '_' and '.', e.g. "DB-AXF-013S", "SPEED_SPEC".</summary>
    public static bool IsValidIdentifier(string? value) =>
        !string.IsNullOrWhiteSpace(value) && IdentifierPattern().IsMatch(value);

    public bool IsReadOnly(string name) => _readOnly.Contains(name);

    /// <summary>
    /// True when ACMS may set <paramref name="name"/> on <paramref name="workstation"/>: the workstation
    /// already has it, it is a known attribute (AWACS leaves empty attributes out of wsdata.xml),
    /// or new attributes are allowed.
    /// </summary>
    public bool IsSettable(string name, Workstation workstation) =>
        _options.AllowNewAttributes || workstation.Attributes.ContainsKey(name) || _known.Contains(name);

    /// <summary>Problems with setting <paramref name="name"/> to <paramref name="value"/>; empty when it is allowed.</summary>
    public List<string> Validate(string name, string? value)
    {
        var errors = new List<string>();

        if (!IsValidIdentifier(name))
        {
            errors.Add($"Attribute name '{name}' is invalid.");
            return errors;
        }

        if (IsReadOnly(name))
        {
            errors.Add($"Attribute '{name}' is read-only in ACMS.");
        }

        value ??= string.Empty;

        if (value.Length > _options.MaxValueLength)
        {
            errors.Add($"Value of '{name}' is longer than {_options.MaxValueLength} characters.");
        }

        if (value.Any(char.IsControl))
        {
            errors.Add($"Value of '{name}' contains control characters.");
        }

        // setwsattr wraps every value in double quotes (attr="value"), so a quote cannot be sent.
        if (value.Contains('"'))
        {
            errors.Add($"Value of '{name}' contains a double quote (\"), which the AWACS update interface cannot carry.");
        }

        return errors;
    }

    [GeneratedRegex(@"^[A-Za-z0-9_\-\.]{1,64}$")]
    private static partial Regex IdentifierPattern();
}

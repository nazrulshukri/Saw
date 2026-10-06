namespace Acms.Core.Services;

/// <summary>Compares attribute sets so ACMS only sends, and only verifies, real differences.</summary>
public static class AttributeDiff
{
    /// <summary>
    /// Returns the requested attributes whose value differs from the current value.
    /// A missing attribute counts as empty, because AWACS leaves empty attributes out of wsdata.xml.
    /// Values are compared trimmed but returned exactly as requested, so a value such as
    /// <c>"B7t,DB09,639, "</c> reaches AWACS unchanged.
    /// </summary>
    public static Dictionary<string, string> Changes(
        IReadOnlyDictionary<string, string> current,
        IReadOnlyDictionary<string, string> requested)
    {
        var changes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (name, value) in requested)
        {
            current.TryGetValue(name, out var currentValue);

            if (Normalize(currentValue) != Normalize(value))
            {
                changes[name] = value ?? string.Empty;
            }
        }

        return changes;
    }

    /// <summary>
    /// Returns every expected attribute whose re-read value does not match. An attribute AWACS no longer
    /// returns matches an expected empty value (clearing a value removes it from wsdata.xml).
    /// </summary>
    public static List<AttributeMismatch> Verify(
        IReadOnlyDictionary<string, string> after,
        IReadOnlyDictionary<string, string> expected)
    {
        var mismatches = new List<AttributeMismatch>();

        foreach (var (name, expectedValue) in expected)
        {
            after.TryGetValue(name, out var actual);

            if (Normalize(actual) != Normalize(expectedValue))
            {
                mismatches.Add(new AttributeMismatch(name, Normalize(expectedValue), actual));
            }
        }

        return mismatches;
    }

    /// <summary>Picks the current values of <paramref name="names"/> (null when the attribute is missing).</summary>
    public static Dictionary<string, string?> Select(
        IReadOnlyDictionary<string, string> source,
        IEnumerable<string> names)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in names)
        {
            result[name] = source.TryGetValue(name, out var value) ? value : null;
        }

        return result;
    }

    // AWACS XML is pretty-printed, so surrounding whitespace is not significant.
    private static string Normalize(string? value) => (value ?? string.Empty).Trim();
}

public sealed record AttributeMismatch(string Name, string Expected, string? Actual);

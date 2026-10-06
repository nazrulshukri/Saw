namespace Acms.Core.Import;

/// <summary>Helpers for pasted text and cell values.</summary>
public static class ImportText
{
    private static readonly HashSet<string> NoValueWords = new(StringComparer.OrdinalIgnoreCase) { "N/A", "NA", "-" };

    /// <summary>
    /// Splits pasted text into rows. Rows copied from Excel or an e-mail table are tab separated;
    /// other lines are split at the first space, comma or semicolon into WSID and value,
    /// e.g. <c>DB-AXF-012S 28000</c> or <c>DB-AXF-013S,ATX18II,Flex</c>.
    /// </summary>
    public static List<IReadOnlyList<string?>> SplitPasted(string? text)
    {
        var rows = new List<IReadOnlyList<string?>>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return rows;
        }

        foreach (var raw in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (line.Contains('\t'))
            {
                rows.Add(line.Split('\t'));
                continue;
            }

            var split = line.AsSpan().IndexOfAny([' ', ' ', ',', ';']);
            rows.Add(split < 0 ? [line] : [line[..split], line[(split + 1)..]]);
        }

        return rows;
    }

    /// <summary>Trims a cell; empty, "N/A", "NA" and "-" mean "no new value" and become null.</summary>
    public static string? NormalizeValue(string? value)
    {
        value = value?.Trim();
        return string.IsNullOrEmpty(value) || NoValueWords.Contains(value) ? null : value;
    }
}

/// <summary>A file or text that could not be read as a table. The message is shown to the user.</summary>
public sealed class ImportException : Exception
{
    public ImportException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

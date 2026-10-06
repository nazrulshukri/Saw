namespace Acms.Core.Services;

/// <summary>One pasted line: a WSID and the new value (null when the line gives none).</summary>
public sealed record BulkInputLine(int LineNumber, string WsId, string? Value);

/// <summary>Reads the "WSID and new value" list pasted on the bulk update page.</summary>
public static class BulkInputParser
{
    private static readonly HashSet<string> HeaderWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "ws", "wsid", "machine", "workstation",
    };

    private static readonly HashSet<string> NoValueWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "N/A", "NA", "-",
    };

    /// <summary>
    /// Rows copied from Excel or an e-mail table are tab separated: the first cell is the WSID and the
    /// last cell the new value, so the columns in between (package, state, old speed...) are ignored.
    /// Other lines are split at the first space, comma or semicolon: <c>DB-AXF-012S 28000</c>.
    /// A header row (first cell "Ws", "WSID", ...) and blank lines are skipped.
    /// </summary>
    public static List<BulkInputLine> Parse(string? text)
    {
        var result = new List<BulkInputLine>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return result;
        }

        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0)
            {
                continue;
            }

            string wsId;
            string? value;

            if (line.Contains('\t'))
            {
                var cells = line.Split('\t').Select(c => c.Trim()).Where(c => c.Length > 0).ToArray();
                wsId = cells[0];
                value = cells.Length > 1 ? cells[^1] : null;
            }
            else
            {
                var split = line.AsSpan().IndexOfAny([' ', ' ', ',', ';']);
                wsId = split < 0 ? line : line[..split];
                value = split < 0 ? null : line[(split + 1)..];
            }

            if (HeaderWords.Contains(wsId))
            {
                continue;
            }

            result.Add(new BulkInputLine(i + 1, wsId, NormalizeValue(value)));
        }

        return result;
    }

    /// <summary>Trims the value; empty, "N/A", "NA" and "-" mean "no new value" and become null.</summary>
    public static string? NormalizeValue(string? value)
    {
        value = value?.Trim();
        return string.IsNullOrEmpty(value) || NoValueWords.Contains(value) ? null : value;
    }
}

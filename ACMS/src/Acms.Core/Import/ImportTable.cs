using System.Text.RegularExpressions;
using Acms.Core.Services;

namespace Acms.Core.Import;

/// <summary>
/// A table read from an uploaded file (Excel, Word, PDF, e-mail, CSV) or pasted text:
/// column names and data rows, all of the same width.
/// </summary>
public sealed partial record ImportTable(
    string Source,
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string>> Rows,
    bool HasHeader)
{
    /// <summary>Column titles that mark the machine column, e.g. "Ws" in the IE UPH table.</summary>
    public static readonly IReadOnlySet<string> WsIdHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "ws", "wsid", "ws id", "machine", "machine id", "workstation", "workstation id",
    };

    public static ImportTable Empty(string source) => new(source, [], [], false);

    /// <summary>Index of the machine column: the column titled Ws/WSID/Machine, otherwise the first column.</summary>
    public int WsIdColumn
    {
        get
        {
            var index = HasHeader ? Headers.ToList().FindIndex(WsIdHeaders.Contains) : -1;
            return Math.Max(0, index);
        }
    }

    /// <summary>
    /// Builds a table from raw rows. The header is the first row (within the first 15) that has a
    /// Ws/WSID/Machine cell, so title rows above it, such as "Period | Package | 3x Hours", are skipped.
    /// Without such a row there is no header and the columns are called "Column 1", "Column 2"...
    /// </summary>
    public static ImportTable FromRows(string source, IEnumerable<IReadOnlyList<string?>> rawRows)
    {
        var rows = rawRows
            .Select(r => r.Select(Clean).ToList())
            .Where(r => r.Any(c => c.Length > 0))
            .ToList();

        if (rows.Count == 0)
        {
            return Empty(source);
        }

        var headerIndex = rows.Take(15).ToList().FindIndex(r => r.Any(WsIdHeaders.Contains));
        var header = headerIndex >= 0 ? rows[headerIndex] : [];
        var data = headerIndex >= 0 ? rows.Skip(headerIndex + 1).ToList() : rows;

        // Drop trailing columns that have neither a title nor any value (e.g. a trailing tab).
        var width = rows.Max(r => r.Count);
        while (width > 1
               && (width > header.Count || header[width - 1].Length == 0)
               && data.All(r => r.Count < width || r[width - 1].Length == 0))
        {
            width--;
        }

        var headers = Enumerable.Range(0, width)
            .Select(i => i < header.Count && header[i].Length > 0 ? header[i] : $"Column {i + 1}")
            .ToList();

        var padded = data
            .Select(r => (IReadOnlyList<string>)Enumerable.Range(0, width).Select(i => i < r.Count ? r[i] : string.Empty).ToList())
            .ToList();

        return new ImportTable(source, headers, padded, headerIndex >= 0);
    }

    /// <summary>
    /// Picks the table that most likely holds the update list (a file can contain several, e.g. an
    /// e-mail with a signature table): tables with a Ws/WSID header first, then the most machine-like ids.
    /// </summary>
    public static ImportTable Best(string source, IEnumerable<IEnumerable<IReadOnlyList<string?>>> tables)
    {
        ImportTable? best = null;
        var bestScore = -1;

        foreach (var candidate in tables)
        {
            var table = FromRows(source, candidate);
            if (table.Rows.Count == 0)
            {
                continue;
            }

            var column = table.WsIdColumn;
            var score = (table.HasHeader ? 100_000 : 0) + table.Rows.Count(r => LooksLikeWsId(r[column]));
            if (score > bestScore)
            {
                best = table;
                bestScore = score;
            }
        }

        return best ?? Empty(source);
    }

    /// <summary>A plausible machine id such as DB-AXF-012S: a valid identifier containing a digit.</summary>
    public static bool LooksLikeWsId(string value) => EditRules.IsValidIdentifier(value) && value.Any(char.IsDigit);

    private static string Clean(string? cell) => Whitespace().Replace(cell ?? string.Empty, " ").Trim();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}

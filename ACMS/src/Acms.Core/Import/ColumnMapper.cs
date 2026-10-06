using System.Text.RegularExpressions;
using Acms.Core.Services;

namespace Acms.Core.Import;

/// <summary>
/// Decides which column holds the machine (WSID) and which AWACS attribute every other column sets.
/// </summary>
public static partial class ColumnMapper
{
    /// <summary>Mapping target of the machine column.</summary>
    public const string WsId = "WSID";

    /// <summary>
    /// Suggested target per column: <see cref="WsId"/>, an attribute name, or "" (ignored).
    /// A column is mapped when its title is a known attribute ("SPEED_SPEC", "Speed spec") or a
    /// configured alias ("Change T Speed" = SPEED_SPEC). Everything else (Package, State, old speed...)
    /// is ignored until the user chooses otherwise.
    /// </summary>
    public static List<string> Suggest(ImportTable table, ImportOptions options, IEnumerable<string> knownAttributes)
    {
        var known = new HashSet<string>(knownAttributes, StringComparer.OrdinalIgnoreCase);
        var aliases = new Dictionary<string, string>(options.ColumnAliases, StringComparer.OrdinalIgnoreCase);
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>(table.Headers.Count);

        for (var i = 0; i < table.Headers.Count; i++)
        {
            var target = string.Empty;

            if (i == table.WsIdColumn)
            {
                target = WsId;
            }
            else if (table.HasHeader)
            {
                var header = table.Headers[i].Trim();
                if (aliases.TryGetValue(header, out var alias))
                {
                    target = alias.Trim().ToUpperInvariant();
                }
                else if (known.Contains(Normalize(header)))
                {
                    target = Normalize(header);
                }
            }

            // One column per attribute: the first one wins.
            result.Add(target.Length > 0 && !used.Add(target) ? string.Empty : target);
        }

        return result;
    }

    /// <summary>Turns the table into one row per machine using <paramref name="mapping"/>.</summary>
    /// <returns>The rows, or the problems with the mapping.</returns>
    public static (List<BulkInputRow> Rows, List<string> Errors) ToRows(ImportTable table, IReadOnlyList<string?> mapping)
    {
        var targets = Enumerable.Range(0, table.Headers.Count)
            .Select(i => i < mapping.Count ? (mapping[i] ?? string.Empty).Trim().ToUpperInvariant() : string.Empty)
            .ToArray();

        var errors = new List<string>();
        var wsColumns = Enumerable.Range(0, targets.Length).Where(i => targets[i] == WsId).ToList();
        var valueColumns = Enumerable.Range(0, targets.Length).Where(i => targets[i].Length > 0 && targets[i] != WsId).ToList();

        if (wsColumns.Count != 1)
        {
            errors.Add("Mark exactly one column as WSID (the machine).");
        }

        if (valueColumns.Count == 0)
        {
            errors.Add("Choose at least one column to write to AWACS, for example SPEED_SPEC.");
        }

        foreach (var duplicate in valueColumns.GroupBy(i => targets[i]).Where(g => g.Count() > 1))
        {
            errors.Add($"More than one column is set to {duplicate.Key}.");
        }

        foreach (var invalid in valueColumns.Select(i => targets[i]).Distinct().Where(t => !EditRules.IsValidIdentifier(t)))
        {
            errors.Add($"'{invalid}' is not a valid attribute name.");
        }

        if (errors.Count > 0)
        {
            return ([], errors);
        }

        var wsColumn = wsColumns[0];
        var rows = new List<BulkInputRow>();

        for (var r = 0; r < table.Rows.Count; r++)
        {
            var cells = table.Rows[r];
            var wsId = cells[wsColumn].Trim();

            // Blank lines and header rows repeated on later pages are not machines.
            if (wsId.Length == 0 || ImportTable.WsIdHeaders.Contains(wsId))
            {
                continue;
            }

            var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var column in valueColumns)
            {
                values[targets[column]] = ImportText.NormalizeValue(cells[column]);
            }

            rows.Add(new BulkInputRow(r + 1, wsId, values));
        }

        return (rows, []);
    }

    /// <summary>"Speed spec" and "speed-spec" become SPEED_SPEC.</summary>
    public static string Normalize(string header) => Separators().Replace(header.Trim(), "_").ToUpperInvariant();

    [GeneratedRegex(@"[\s\-]+")]
    private static partial Regex Separators();
}

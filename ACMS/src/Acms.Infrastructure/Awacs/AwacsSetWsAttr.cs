namespace Acms.Infrastructure.Awacs;

public enum AwacsAttributeFormat
{
    /// <summary><c>setwsattr=WsId="WSID",attr="value",attr2="value2"</c></summary>
    Quoted,

    /// <summary><c>setwsattr=WsId:WSID,attr:value,attr2:value2</c></summary>
    Colon,
}

/// <summary>
/// Builds the <c>setwsattr</c> argument of <c>/template/wswoupdate.html?ws=WSID&amp;setwsattr=...</c>,
/// the documented interface behind "Save changes" on the AWACS edit workstation page.
/// </summary>
public static class AwacsSetWsAttr
{
    /// <exception cref="ArgumentException">A value cannot be written in the chosen format.</exception>
    public static string Build(string wsId, IReadOnlyDictionary<string, string> changes, AwacsAttributeFormat format)
    {
        var pairs = new List<string>(changes.Count + 1) { Pair("WsId", wsId, format) };
        pairs.AddRange(changes.Select(c => Pair(c.Key, c.Value ?? string.Empty, format)));
        return string.Join(",", pairs);
    }

    private static string Pair(string name, string value, AwacsAttributeFormat format)
    {
        if (format == AwacsAttributeFormat.Colon)
        {
            if (value.Contains(','))
            {
                throw new ArgumentException(
                    $"The value of {name} contains a comma, which the Colon format cannot carry. Use the Quoted format.");
            }

            return $"{name}:{value}";
        }

        if (value.Contains('"'))
        {
            throw new ArgumentException($"The value of {name} contains a double quote, which AWACS cannot carry.");
        }

        return $"{name}=\"{value}\"";
    }
}

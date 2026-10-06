namespace Acms.Core.Domain;

/// <summary>
/// One AWACS workstation (equipment / machine) and its attributes, as returned by
/// <c>/template/wsdata.xml?ws=WSID</c>.
/// </summary>
public sealed class Workstation
{
    private readonly Dictionary<string, string> _attributes;

    public Workstation(string wsId, IEnumerable<KeyValuePair<string, string>>? attributes = null)
    {
        WsId = wsId;
        _attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (attributes is null)
        {
            return;
        }

        foreach (var (name, value) in attributes)
        {
            // First value wins if AWACS repeats an element name.
            _attributes.TryAdd(name, value);
        }
    }

    public string WsId { get; }

    /// <summary>Attribute name to value. Names are case-insensitive.</summary>
    public IReadOnlyDictionary<string, string> Attributes => _attributes;

    public string? Get(string name) => _attributes.TryGetValue(name, out var value) ? value : null;
}

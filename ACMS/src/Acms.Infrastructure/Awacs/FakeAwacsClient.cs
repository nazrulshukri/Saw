using System.Collections.Concurrent;
using Acms.Core.Abstractions;
using Acms.Core.Domain;

namespace Acms.Infrastructure.Awacs;

/// <summary>
/// In-memory AWACS stand-in so the UI can be developed and demonstrated without a real server.
/// Registered only when <c>Awacs:UseFake</c> is true in the Development environment.
/// </summary>
public sealed class FakeAwacsClient : IAwacsClient
{
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<string, Dictionary<string, string>>> _data = new();

    public Task<IReadOnlyList<Workstation>> GetWorkstationsAsync(
        AwacsServer server,
        IReadOnlyCollection<string>? wsIds,
        CancellationToken cancellationToken = default)
    {
        var stations = StationsFor(server);
        IEnumerable<KeyValuePair<string, Dictionary<string, string>>> selected = stations;

        if (wsIds is { Count: > 0 })
        {
            var wanted = new HashSet<string>(wsIds, StringComparer.OrdinalIgnoreCase);
            selected = stations.Where(s => wanted.Contains(s.Key));
        }

        IReadOnlyList<Workstation> result = selected
            .OrderBy(s => s.Key, StringComparer.OrdinalIgnoreCase)
            .Select(s =>
            {
                lock (s.Value)
                {
                    return new Workstation(s.Key, s.Value.ToList());
                }
            })
            .ToList();

        return Task.FromResult(result);
    }

    public async Task<Workstation?> GetWorkstationAsync(AwacsServer server, string wsId, CancellationToken cancellationToken = default)
    {
        var list = await GetWorkstationsAsync(server, [wsId], cancellationToken);
        return list.FirstOrDefault();
    }

    public Task<AwacsUpdateResult> UpdateAttributesAsync(
        AwacsServer server,
        string wsId,
        IReadOnlyDictionary<string, string> changes,
        CancellationToken cancellationToken = default)
    {
        if (!StationsFor(server).TryGetValue(wsId, out var attributes))
        {
            return Task.FromResult(new AwacsUpdateResult(false, $"Unknown workstation {wsId}"));
        }

        lock (attributes)
        {
            foreach (var (name, value) in changes)
            {
                // Like AWACS: an emptied attribute is no longer returned by wsdata.xml.
                if (string.IsNullOrWhiteSpace(value))
                {
                    attributes.Remove(name);
                }
                else
                {
                    attributes[name] = value;
                }
            }

            attributes["UPDATED"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        return Task.FromResult(new AwacsUpdateResult(true, null));
    }

    private ConcurrentDictionary<string, Dictionary<string, string>> StationsFor(AwacsServer server) =>
        _data.GetOrAdd(server.Id, _ => Seed(server));

    // Die bonders like the PH3C machines; odd server ids get the AXF line, even ids the AD3 line.
    private static readonly (string WsId, string Package, string Speed)[] AxfLine =
    [
        ("DB-AXF-012S", "Micropack", "25600"), ("DB-AXF-013S", "Picogate", "36800"),
        ("DB-AXF-014S", "MCD882", "38400"), ("DB-AXF-015S", "MCD882", "38400"),
        ("DB-AXF-017S", "Picogate", "36800"), ("DB-AXF-018S", "MCD882", "38400"),
        ("DB-AXF-023S", "MCD 1220", "25600"), ("DB-AXF-024S", "MCD 1220", "25600"),
    ];

    private static readonly (string WsId, string Package, string Speed)[] Ad3Line =
    [
        ("DB-AD3-020", "Picogate", "24000"), ("DB-AD3-021", "Picogate", "24000"),
        ("DB-AD3-103S", "Micropack", "25600"), ("DB-AD3-104S", "Micropack", "25600"),
        ("DB-AD3-105S", "Micropack", "25600"), ("DB-AD3-106S", "Micropack", "25600"),
        ("DB-AD3-107S", "Micropack", "25600"), ("DB-AD3-108S", "Micropack", "25600"),
    ];

    private static ConcurrentDictionary<string, Dictionary<string, string>> Seed(AwacsServer server)
    {
        var stations = new ConcurrentDictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        var line = server.Id % 2 == 1 ? AxfLine : Ad3Line;
        var prefix = server.Id <= 2 ? string.Empty : $"S{server.Id}-";

        for (var i = 0; i < line.Length; i++)
        {
            var (name, package, speed) = line[i];
            var wsId = prefix + name;
            var computer = wsId.ToLowerInvariant();

            // Empty attributes are left out, as AWACS does.
            stations[wsId] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["WSID"] = wsId,
                ["COMPUTER"] = computer,
                ["CONTROL"] = "ATX18II,Flex",
                ["EQUIPMENT"] = "Diebond",
                ["IPADDRESS"] = $"172.16.{200 + server.Id}.{10 + i}",
                ["LOCATION"] = "PH3C",
                ["MODEL"] = "XF_DBSG",
                ["SPEED_SPEC"] = speed,
                ["SRCFILE"] = $@"\\{computer}\C$\Itec\Work\{wsId}.esm",
                ["UPDATED"] = "2026-10-01 08:00:00",
                ["VERSION"] = "x64_V2026.04.09",
                ["WO_PACKAGE"] = package,
            };
        }

        return stations;
    }
}

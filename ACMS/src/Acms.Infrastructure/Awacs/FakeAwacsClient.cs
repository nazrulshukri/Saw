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
                attributes[name] = value;
            }
        }

        return Task.FromResult(new AwacsUpdateResult(true, null));
    }

    private ConcurrentDictionary<string, Dictionary<string, string>> StationsFor(AwacsServer server) =>
        _data.GetOrAdd(server.Id, _ => Seed(server));

    private static ConcurrentDictionary<string, Dictionary<string, string>> Seed(AwacsServer server)
    {
        var stations = new ConcurrentDictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        string[] types = ["L200", "WPROBER", "ADAT", "MCDWB"];
        string[] states = ["IDLE", "RUNNING", "FINISHED"];

        for (var i = 1; i <= 8; i++)
        {
            var wsId = $"S{server.Id}-EQ-{i:000}";
            stations[wsId] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["WSID"] = wsId,
                ["WSTYPE"] = types[i % types.Length],
                ["STATE"] = states[i % states.Length],
                ["RECIPELOAD"] = $"RCP_{i:00}",
                ["LOCATION"] = $"Bay {(i + 1) / 2}",
                ["MATERIALCHECK"] = i % 2 == 0 ? "ON" : "OFF",
            };
        }

        return stations;
    }
}

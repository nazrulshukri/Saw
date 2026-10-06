using Acms.Core.Abstractions;
using Acms.Core.Domain;

namespace Acms.Tests;

internal sealed class InMemoryServerRepository : IServerRepository
{
    public List<AwacsServer> Servers { get; } = [];

    public Task<IReadOnlyList<AwacsServer>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AwacsServer>>(Servers.Where(s => includeInactive || s.IsActive).ToList());

    public Task<AwacsServer?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Servers.FirstOrDefault(s => s.Id == id));

    public Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Servers.Any(s => s.Name == name && s.Id != excludeId));

    public Task AddAsync(AwacsServer server, CancellationToken cancellationToken = default)
    {
        server.Id = Servers.Count == 0 ? 1 : Servers.Max(s => s.Id) + 1;
        Servers.Add(server);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(AwacsServer server, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

internal sealed class InMemoryAuditLog : IAuditLog
{
    public List<AuditEntry> Entries { get; } = [];
    public bool FailWrites { get; set; }

    public Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        if (FailWrites)
        {
            throw new InvalidOperationException("database down");
        }

        Entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<PagedResult<AuditEntry>> QueryAsync(AuditQuery query, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PagedResult<AuditEntry>(Entries, Entries.Count, 1, Entries.Count));
}

/// <summary>AWACS stand-in whose behaviour each test can script. Behaves like AWACS for empty values.</summary>
internal sealed class ScriptedAwacsClient : IAwacsClient
{
    private readonly Dictionary<int, Dictionary<string, Dictionary<string, string>>> _servers = [];

    /// <summary>Workstations of server 1.</summary>
    public Dictionary<string, Dictionary<string, string>> Stations => For(1);

    /// <summary>When true the update call reports success but changes nothing.</summary>
    public bool IgnoreUpdates { get; set; }

    public Exception? ThrowOnUpdate { get; set; }
    public Exception? ThrowOnRead { get; set; }

    /// <summary>Servers whose reads fail.</summary>
    public HashSet<int> UnreachableServers { get; } = [];

    public int UpdateCalls { get; private set; }

    public Dictionary<string, Dictionary<string, string>> For(int serverId)
    {
        if (!_servers.TryGetValue(serverId, out var stations))
        {
            _servers[serverId] = stations = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        }

        return stations;
    }

    public void Add(int serverId, string wsId, params (string Name, string Value)[] attributes)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["WSID"] = wsId };
        foreach (var (name, value) in attributes)
        {
            values[name] = value;
        }

        For(serverId)[wsId] = values;
    }

    public Task<IReadOnlyList<Workstation>> GetWorkstationsAsync(AwacsServer server, IReadOnlyCollection<string>? wsIds, CancellationToken cancellationToken = default)
    {
        if (ThrowOnRead is not null)
        {
            throw ThrowOnRead;
        }

        if (UnreachableServers.Contains(server.Id))
        {
            throw new HttpRequestException($"{server.Name} is unreachable");
        }

        IReadOnlyList<Workstation> list = For(server.Id)
            .Where(s => wsIds is null || wsIds.Count == 0 || wsIds.Contains(s.Key, StringComparer.OrdinalIgnoreCase))
            .Select(s => new Workstation(s.Key, s.Value))
            .ToList();
        return Task.FromResult(list);
    }

    public async Task<Workstation?> GetWorkstationAsync(AwacsServer server, string wsId, CancellationToken cancellationToken = default) =>
        (await GetWorkstationsAsync(server, [wsId], cancellationToken)).FirstOrDefault();

    public Task<AwacsUpdateResult> UpdateAttributesAsync(AwacsServer server, string wsId, IReadOnlyDictionary<string, string> changes, CancellationToken cancellationToken = default)
    {
        UpdateCalls++;

        if (ThrowOnUpdate is not null)
        {
            throw ThrowOnUpdate;
        }

        if (!IgnoreUpdates)
        {
            var attributes = For(server.Id)[wsId];
            foreach (var (name, value) in changes)
            {
                // AWACS leaves empty attributes out of wsdata.xml.
                if (string.IsNullOrWhiteSpace(value))
                {
                    attributes.Remove(name);
                }
                else
                {
                    attributes[name] = value;
                }
            }
        }

        return Task.FromResult(new AwacsUpdateResult(true, null));
    }
}

/// <summary>Records requests and replies with queued responses.</summary>
internal sealed class StubHttpHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses = new();

    public List<Uri> Requests { get; } = [];

    public void Enqueue(System.Net.HttpStatusCode status, string body = "") =>
        _responses.Enqueue(_ => new HttpResponseMessage(status) { Content = new StringContent(body) });

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request.RequestUri!);
        var response = _responses.Count > 0
            ? _responses.Dequeue()(request)
            : new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent("<wsdata/>") };
        return Task.FromResult(response);
    }
}

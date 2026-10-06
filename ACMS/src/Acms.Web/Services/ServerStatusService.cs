using Acms.Core.Abstractions;
using Acms.Core.Domain;
using Microsoft.Extensions.Caching.Memory;

namespace Acms.Web.Services;

public sealed record ServerStatus(AwacsServer Server, bool Reachable, int? WorkstationCount, string? Error, DateTime CheckedUtc);

/// <summary>Reachability and workstation counts for the dashboard, cached briefly per server.</summary>
public sealed class ServerStatusService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(15);

    private readonly IAwacsClient _awacs;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ServerStatusService> _logger;

    public ServerStatusService(IAwacsClient awacs, IMemoryCache cache, ILogger<ServerStatusService> logger)
    {
        _awacs = awacs;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ServerStatus>> GetAsync(
        IEnumerable<AwacsServer> servers,
        bool refresh,
        CancellationToken cancellationToken)
    {
        return await Task.WhenAll(servers.Select(s => GetOneAsync(s, refresh, cancellationToken)));
    }

    private async Task<ServerStatus> GetOneAsync(AwacsServer server, bool refresh, CancellationToken cancellationToken)
    {
        var key = $"acms:status:{server.Id}:{server.BaseUrl}";

        if (!refresh && _cache.TryGetValue(key, out ServerStatus? cached) && cached is not null)
        {
            return cached with { Server = server };
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(CheckTimeout);

        ServerStatus status;
        try
        {
            var workstations = await _awacs.GetWorkstationsAsync(server, null, timeout.Token);
            status = new ServerStatus(server, true, workstations.Count, null, DateTime.UtcNow);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            status = new ServerStatus(server, false, null, $"No answer within {CheckTimeout.TotalSeconds:0} s", DateTime.UtcNow);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Status check failed for {Server}", server.Name);
            status = new ServerStatus(server, false, null, ex.Message, DateTime.UtcNow);
        }

        _cache.Set(key, status, CacheDuration);
        return status;
    }
}

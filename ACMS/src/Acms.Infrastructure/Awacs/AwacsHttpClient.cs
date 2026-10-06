using System.Net;
using Acms.Core.Abstractions;
using Acms.Core.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Acms.Infrastructure.Awacs;

/// <summary>Real AWACS connector over HTTP/XML. Uses only the documented AWACS interfaces.</summary>
public sealed class AwacsHttpClient : IAwacsClient
{
    private readonly HttpClient _http;
    private readonly AwacsOptions _options;
    private readonly ILogger<AwacsHttpClient> _logger;

    public AwacsHttpClient(HttpClient http, IOptions<AwacsOptions> options, ILogger<AwacsHttpClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Workstation>> GetWorkstationsAsync(
        AwacsServer server,
        IReadOnlyCollection<string>? wsIds,
        CancellationToken cancellationToken = default)
    {
        // "*" and "," are part of the AWACS syntax, so only the ids themselves are encoded.
        var wsParameter = wsIds is null || wsIds.Count == 0
            ? "*"
            : string.Join(",", wsIds.Select(Uri.EscapeDataString));

        var uri = BuildUri(server, _options.WorkstationDataPath, "ws=" + wsParameter);
        var xml = await GetWithRetryAsync(uri, cancellationToken);
        var requested = wsIds?.Count == 1 ? wsIds.First() : null;

        return AwacsXmlParser.Parse(xml, _options, requested);
    }

    public async Task<Workstation?> GetWorkstationAsync(
        AwacsServer server,
        string wsId,
        CancellationToken cancellationToken = default)
    {
        var workstations = await GetWorkstationsAsync(server, [wsId], cancellationToken);
        return workstations.FirstOrDefault(w => string.Equals(w.WsId, wsId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<AwacsUpdateResult> UpdateAttributesAsync(
        AwacsServer server,
        string wsId,
        IReadOnlyDictionary<string, string> changes,
        CancellationToken cancellationToken = default)
    {
        foreach (var (name, value) in changes)
        {
            var query = _options.UpdateQueryTemplate
                .Replace("{ws}", Uri.EscapeDataString(wsId), StringComparison.Ordinal)
                .Replace("{name}", Uri.EscapeDataString(name), StringComparison.Ordinal)
                .Replace("{value}", Uri.EscapeDataString(value), StringComparison.Ordinal);

            var uri = BuildUri(server, _options.UpdatePath, query);
            _logger.LogInformation("AWACS update {Server} {WsId}: {Attribute}", server.Name, wsId, name);

            // Updates are never retried automatically: the re-read decides what happened.
            using var response = await _http.GetAsync(uri, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new AwacsUpdateResult(false,
                    $"HTTP {(int)response.StatusCode} {response.ReasonPhrase} while setting {name}");
            }

            var marker = _options.UpdateFailureMarkers
                .FirstOrDefault(m => !string.IsNullOrEmpty(m) && body.Contains(m, StringComparison.OrdinalIgnoreCase));

            if (marker is not null)
            {
                return new AwacsUpdateResult(false, $"AWACS response for {name} contains '{marker}': {Truncate(body)}");
            }
        }

        return new AwacsUpdateResult(true, null);
    }

    private static Uri BuildUri(AwacsServer server, string relativePath, string query)
    {
        var baseUrl = server.BaseUrl.EndsWith('/') ? server.BaseUrl : server.BaseUrl + "/";
        var builder = new UriBuilder(new Uri(new Uri(baseUrl), relativePath.TrimStart('/')))
        {
            Query = query,
        };

        return builder.Uri;
    }

    private async Task<string> GetWithRetryAsync(Uri uri, CancellationToken cancellationToken)
    {
        var attempt = 0;

        while (true)
        {
            attempt++;

            try
            {
                using var response = await _http.GetAsync(uri, cancellationToken);

                if (response.StatusCode >= HttpStatusCode.InternalServerError && attempt <= _options.ReadRetryCount)
                {
                    _logger.LogWarning("AWACS read {Uri} returned {Status}, retrying ({Attempt})", uri, (int)response.StatusCode, attempt);
                }
                else
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new AwacsException($"AWACS returned HTTP {(int)response.StatusCode} {response.ReasonPhrase} for {uri.AbsolutePath}");
                    }

                    return await response.Content.ReadAsStringAsync(cancellationToken);
                }
            }
            catch (Exception ex) when (IsTransient(ex, cancellationToken) && attempt <= _options.ReadRetryCount)
            {
                _logger.LogWarning(ex, "AWACS read {Uri} failed, retrying ({Attempt})", uri, attempt);
            }
            catch (Exception ex) when (IsTransient(ex, cancellationToken))
            {
                throw new AwacsException($"Could not reach AWACS at {uri.Host}: {ex.Message}", ex);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt), cancellationToken);
        }
    }

    // Network errors and HttpClient timeouts; not the caller cancelling.
    private static bool IsTransient(Exception ex, CancellationToken cancellationToken) =>
        ex is HttpRequestException
        || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested);

    private static string Truncate(string text) => text.Length <= 300 ? text : text[..300] + "...";
}

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
        if (wsIds is null || wsIds.Count == 0)
        {
            var all = await GetWithRetryAsync(BuildUri(server, _options.WorkstationDataPath, "ws=*"), cancellationToken);
            return AwacsXmlParser.Parse(all, _options);
        }

        // "," is part of the AWACS syntax, so only the ids themselves are encoded.
        var result = new List<Workstation>();
        foreach (var chunk in wsIds.Distinct(StringComparer.OrdinalIgnoreCase).Chunk(Math.Max(1, _options.MaxIdsPerRequest)))
        {
            var uri = BuildUri(server, _options.WorkstationDataPath, "ws=" + string.Join(",", chunk.Select(Uri.EscapeDataString)));
            var xml = await GetWithRetryAsync(uri, cancellationToken);
            result.AddRange(AwacsXmlParser.Parse(xml, _options, chunk.Length == 1 ? chunk[0] : null));
        }

        return result;
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
        string setWsAttr;
        try
        {
            setWsAttr = AwacsSetWsAttr.Build(wsId, changes, _options.UpdateAttributeFormat);
        }
        catch (ArgumentException ex)
        {
            return new AwacsUpdateResult(false, ex.Message);
        }

        var login = await LoginAsync(server, cancellationToken);
        if (login is not null)
        {
            return new AwacsUpdateResult(false, login);
        }

        var uri = BuildUri(server, _options.UpdatePath,
            "ws=" + Uri.EscapeDataString(wsId) + "&setwsattr=" + Uri.EscapeDataString(setWsAttr));

        _logger.LogInformation("AWACS update {Server} {WsId}: {Attributes}", server.Name, wsId, string.Join(", ", changes.Keys));

        // Updates are never retried automatically: the re-read decides what happened.
        using var response = await _http.GetAsync(uri, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new AwacsUpdateResult(false, $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
        }

        var marker = _options.UpdateFailureMarkers
            .FirstOrDefault(m => !string.IsNullOrEmpty(m) && body.Contains(m, StringComparison.OrdinalIgnoreCase));

        return marker is null
            ? new AwacsUpdateResult(true, null)
            : new AwacsUpdateResult(false, $"AWACS response contains '{marker}': {Truncate(body)}");
    }

    /// <summary>
    /// Logs in on the AWACS server when a user is configured. The session is kept in the cookies of
    /// this HttpClient. Returns an error text, or null when logged in (or no login is configured).
    /// </summary>
    private async Task<string?> LoginAsync(AwacsServer server, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_options.Username))
        {
            return null;
        }

        var uri = BuildUri(server, _options.LoginPath,
            "Awacs_Username=" + Uri.EscapeDataString(_options.Username)
            + "&Awacs_password=" + Uri.EscapeDataString(_options.Password ?? string.Empty));

        try
        {
            using var response = await _http.GetAsync(uri, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            // A logged-in AWACS page offers "Logout"; the login form has a password field instead.
            if (response.IsSuccessStatusCode && body.Contains("Logout", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            _logger.LogWarning("AWACS login as {User} on {Server} failed (HTTP {Status})", _options.Username, server.Name, (int)response.StatusCode);
            return $"Could not log in to AWACS '{server.Name}' as '{_options.Username}'. Check Awacs:Username and Awacs:Password.";
        }
        catch (HttpRequestException ex)
        {
            return $"Could not reach AWACS '{server.Name}' to log in: {ex.Message}";
        }
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

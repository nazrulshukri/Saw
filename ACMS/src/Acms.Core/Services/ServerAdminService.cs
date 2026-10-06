using System.Text.Json;
using Acms.Core.Abstractions;
using Acms.Core.Domain;
using Microsoft.Extensions.Logging;

namespace Acms.Core.Services;

public sealed record ServerInput(string Name, string BaseUrl, string? Description);

public sealed record ServerAdminResult(bool Succeeded, IReadOnlyList<string> Errors, AwacsServer? Server)
{
    public static ServerAdminResult Fail(params string[] errors) => new(false, errors, null);
    public static ServerAdminResult Ok(AwacsServer server) => new(true, [], server);
}

/// <summary>Adds and changes AWACS server connections. Every change is audited.</summary>
public sealed class ServerAdminService
{
    private readonly IServerRepository _servers;
    private readonly IAuditLog _audit;
    private readonly TimeProvider _clock;
    private readonly ILogger<ServerAdminService> _logger;

    public ServerAdminService(
        IServerRepository servers,
        IAuditLog audit,
        TimeProvider clock,
        ILogger<ServerAdminService> logger)
    {
        _servers = servers;
        _audit = audit;
        _clock = clock;
        _logger = logger;
    }

    public async Task<ServerAdminResult> CreateAsync(ServerInput input, string userName, CancellationToken cancellationToken = default)
    {
        var errors = Validate(input, out var baseUrl);
        if (errors.Count == 0 && await _servers.NameExistsAsync(input.Name.Trim(), null, cancellationToken))
        {
            errors.Add($"A server named '{input.Name.Trim()}' already exists.");
        }

        if (errors.Count > 0)
        {
            return new ServerAdminResult(false, errors, null);
        }

        var now = _clock.GetUtcNow().UtcDateTime;
        var server = new AwacsServer
        {
            Name = input.Name.Trim(),
            BaseUrl = baseUrl!,
            Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
            IsActive = true,
            CreatedUtc = now,
            CreatedBy = userName,
        };

        await _servers.AddAsync(server, cancellationToken);
        await WriteAuditAsync(AuditAction.ServerCreate, server, userName, null, Snapshot(server), cancellationToken);
        return ServerAdminResult.Ok(server);
    }

    public async Task<ServerAdminResult> UpdateAsync(int id, ServerInput input, string userName, CancellationToken cancellationToken = default)
    {
        var server = await _servers.GetAsync(id, cancellationToken);
        if (server is null)
        {
            return ServerAdminResult.Fail($"Server {id} does not exist.");
        }

        var errors = Validate(input, out var baseUrl);
        if (errors.Count == 0 && await _servers.NameExistsAsync(input.Name.Trim(), id, cancellationToken))
        {
            errors.Add($"A server named '{input.Name.Trim()}' already exists.");
        }

        if (errors.Count > 0)
        {
            return new ServerAdminResult(false, errors, null);
        }

        var before = Snapshot(server);

        server.Name = input.Name.Trim();
        server.BaseUrl = baseUrl!;
        server.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        server.UpdatedUtc = _clock.GetUtcNow().UtcDateTime;
        server.UpdatedBy = userName;

        await _servers.UpdateAsync(server, cancellationToken);
        await WriteAuditAsync(AuditAction.ServerUpdate, server, userName, before, Snapshot(server), cancellationToken);
        return ServerAdminResult.Ok(server);
    }

    public async Task<ServerAdminResult> SetActiveAsync(int id, bool active, string userName, CancellationToken cancellationToken = default)
    {
        var server = await _servers.GetAsync(id, cancellationToken);
        if (server is null)
        {
            return ServerAdminResult.Fail($"Server {id} does not exist.");
        }

        if (server.IsActive == active)
        {
            return ServerAdminResult.Ok(server);
        }

        var before = Snapshot(server);
        server.IsActive = active;
        server.UpdatedUtc = _clock.GetUtcNow().UtcDateTime;
        server.UpdatedBy = userName;

        await _servers.UpdateAsync(server, cancellationToken);
        await WriteAuditAsync(
            active ? AuditAction.ServerActivate : AuditAction.ServerDeactivate,
            server, userName, before, Snapshot(server), cancellationToken);
        return ServerAdminResult.Ok(server);
    }

    private static List<string> Validate(ServerInput input, out string? normalizedBaseUrl)
    {
        var errors = new List<string>();
        normalizedBaseUrl = null;

        if (string.IsNullOrWhiteSpace(input.Name))
        {
            errors.Add("Name is required.");
        }
        else if (input.Name.Trim().Length > 100)
        {
            errors.Add("Name must be 100 characters or fewer.");
        }

        if (!Uri.TryCreate(input.BaseUrl?.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            errors.Add("Base URL must be an absolute http:// or https:// address.");
        }
        else if (!string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
        {
            errors.Add("Base URL must not contain a query string or fragment.");
        }
        else
        {
            // People paste a page address such as .../template/general/status.html; keep only the site root.
            var text = uri.GetLeftPart(UriPartial.Path);
            var template = text.IndexOf("/template/", StringComparison.OrdinalIgnoreCase);
            if (template >= 0)
            {
                text = text[..template];
            }

            normalizedBaseUrl = text.EndsWith('/') ? text : text + "/";

            if (normalizedBaseUrl.Length > 400)
            {
                errors.Add("Base URL must be 400 characters or fewer.");
            }
        }

        if (input.Description?.Length > 500)
        {
            errors.Add("Description must be 500 characters or fewer.");
        }

        return errors;
    }

    private async Task WriteAuditAsync(
        AuditAction action,
        AwacsServer server,
        string userName,
        object? before,
        object after,
        CancellationToken cancellationToken)
    {
        var entry = new AuditEntry
        {
            TimestampUtc = _clock.GetUtcNow().UtcDateTime,
            UserName = userName,
            Action = action,
            Outcome = AuditOutcome.Success,
            ServerId = server.Id,
            ServerName = server.Name,
            BeforeJson = before is null ? null : JsonSerializer.Serialize(before),
            AfterJson = JsonSerializer.Serialize(after),
            Message = $"{action} '{server.Name}'",
            CorrelationId = Guid.NewGuid().ToString("N"),
        };

        try
        {
            await _audit.WriteAsync(entry, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Audit record could not be written for {Action} on server {ServerId}", action, server.Id);
        }
    }

    private static object Snapshot(AwacsServer server) => new
    {
        server.Name,
        server.BaseUrl,
        server.Description,
        server.IsActive,
    };
}

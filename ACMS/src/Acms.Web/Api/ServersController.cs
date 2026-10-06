using Acms.Core.Abstractions;
using Acms.Core.Domain;
using Acms.Core.Security;
using Acms.Core.Services;
using Acms.Infrastructure.Awacs;
using Acms.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Acms.Web.Api;

[ApiController]
[Route("api/servers")]
[Authorize(Policy = AcmsPolicies.CanView)]
public sealed class ServersController : ControllerBase
{
    private readonly IServerRepository _servers;
    private readonly IAwacsClient _awacs;
    private readonly EquipmentChangeService _changes;

    public ServersController(IServerRepository servers, IAwacsClient awacs, EquipmentChangeService changes)
    {
        _servers = servers;
        _awacs = awacs;
        _changes = changes;
    }

    /// <summary>Active AWACS servers.</summary>
    [HttpGet]
    public async Task<IEnumerable<ServerDto>> GetServers(CancellationToken cancellationToken)
    {
        var servers = await _servers.GetAllAsync(includeInactive: false, cancellationToken);
        return servers.Select(ServerDto.From);
    }

    /// <summary>Workstations on one server. <paramref name="ws"/> is a comma-separated list; omit for all.</summary>
    [HttpGet("{serverId:int}/workstations")]
    public async Task<ActionResult<IEnumerable<WorkstationDto>>> GetWorkstations(
        int serverId,
        [FromQuery] string? ws,
        CancellationToken cancellationToken)
    {
        var server = await GetActiveServerAsync(serverId, cancellationToken);
        if (server is null)
        {
            return NotFound();
        }

        var ids = string.IsNullOrWhiteSpace(ws)
            ? null
            : ws.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        try
        {
            var workstations = await _awacs.GetWorkstationsAsync(server, ids, cancellationToken);
            return Ok(workstations.Select(w => WorkstationDto.From(server, w)));
        }
        catch (AwacsException ex)
        {
            return AwacsUnavailable(server, ex);
        }
    }

    [HttpGet("{serverId:int}/workstations/{wsId}")]
    public async Task<ActionResult<WorkstationDto>> GetWorkstation(int serverId, string wsId, CancellationToken cancellationToken)
    {
        var server = await GetActiveServerAsync(serverId, cancellationToken);
        if (server is null)
        {
            return NotFound();
        }

        try
        {
            var workstation = await _awacs.GetWorkstationAsync(server, wsId, cancellationToken);
            return workstation is null ? NotFound() : WorkstationDto.From(server, workstation);
        }
        catch (AwacsException ex)
        {
            return AwacsUnavailable(server, ex);
        }
    }

    /// <summary>
    /// Changes workstation attributes. Returns 200 when applied and verified (or already set),
    /// 400 when rejected, 409 when AWACS changed since <c>expectedOriginal</c> was read,
    /// and 502 when AWACS did not apply the change.
    /// </summary>
    [HttpPut("{serverId:int}/workstations/{wsId}/attributes")]
    [Authorize(Policy = AcmsPolicies.CanEditEquipment)]
    public async Task<ActionResult<EquipmentChangeResult>> UpdateAttributes(
        int serverId,
        string wsId,
        [FromBody] UpdateAttributesRequest body,
        CancellationToken cancellationToken)
    {
        var result = await _changes.EditAsync(
            new EquipmentEditRequest(serverId, wsId, body.Values ?? [], body.ExpectedOriginal),
            User.AcmsUserName(),
            cancellationToken);

        return result.Outcome switch
        {
            AuditOutcome.Success or AuditOutcome.NoChange => Ok(result),
            AuditOutcome.Rejected when result.IsConflict => Conflict(result),
            AuditOutcome.Rejected => BadRequest(result),
            _ => StatusCode(StatusCodes.Status502BadGateway, result),
        };
    }

    private async Task<AwacsServer?> GetActiveServerAsync(int serverId, CancellationToken cancellationToken)
    {
        var server = await _servers.GetAsync(serverId, cancellationToken);
        return server is { IsActive: true } ? server : null;
    }

    private ObjectResult AwacsUnavailable(AwacsServer server, AwacsException ex) =>
        Problem(title: $"AWACS server '{server.Name}' did not respond correctly", detail: ex.Message,
            statusCode: StatusCodes.Status502BadGateway);
}

public sealed record UpdateAttributesRequest(
    Dictionary<string, string>? Values,
    Dictionary<string, string>? ExpectedOriginal);

public sealed record ServerDto(int Id, string Name, string BaseUrl, string? Description)
{
    public static ServerDto From(AwacsServer s) => new(s.Id, s.Name, s.BaseUrl, s.Description);
}

public sealed record WorkstationDto(int ServerId, string ServerName, string WsId, IReadOnlyDictionary<string, string> Attributes)
{
    public static WorkstationDto From(AwacsServer server, Workstation w) => new(server.Id, server.Name, w.WsId, w.Attributes);
}

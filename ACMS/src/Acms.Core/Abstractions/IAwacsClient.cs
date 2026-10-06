using Acms.Core.Domain;

namespace Acms.Core.Abstractions;

/// <summary>
/// HTTP/XML connector to one AWACS server. Only the interfaces AWACS documents are used:
/// <list type="bullet">
///   <item><c>/template/wsdata.xml?ws=WSID</c> (also <c>ws=*</c> and <c>ws=WS1,WS2</c>) to read.</item>
///   <item><c>/template/wswoupdate.html?setwsattr=...</c> to update attributes.</item>
/// </list>
/// Create and Delete are not documented by AWACS and are deliberately not part of this interface.
/// </summary>
public interface IAwacsClient
{
    /// <summary>Reads workstations. Pass null or empty <paramref name="wsIds"/> to read all (<c>ws=*</c>).</summary>
    Task<IReadOnlyList<Workstation>> GetWorkstationsAsync(
        AwacsServer server,
        IReadOnlyCollection<string>? wsIds,
        CancellationToken cancellationToken = default);

    /// <summary>Reads one workstation, or null if AWACS does not know it.</summary>
    Task<Workstation?> GetWorkstationAsync(
        AwacsServer server,
        string wsId,
        CancellationToken cancellationToken = default);

    /// <summary>Sends the attribute changes to AWACS. Does not verify them; the caller re-reads.</summary>
    Task<AwacsUpdateResult> UpdateAttributesAsync(
        AwacsServer server,
        string wsId,
        IReadOnlyDictionary<string, string> changes,
        CancellationToken cancellationToken = default);
}

/// <param name="Accepted">True when AWACS answered every update call without an error.</param>
/// <param name="Detail">Error text from AWACS or the HTTP layer when not accepted.</param>
public sealed record AwacsUpdateResult(bool Accepted, string? Detail);

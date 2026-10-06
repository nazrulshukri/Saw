using Acms.Core.Domain;

namespace Acms.Core.Abstractions;

public interface IServerRepository
{
    Task<IReadOnlyList<AwacsServer>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken = default);

    Task<AwacsServer?> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken cancellationToken = default);

    Task AddAsync(AwacsServer server, CancellationToken cancellationToken = default);

    Task UpdateAsync(AwacsServer server, CancellationToken cancellationToken = default);
}

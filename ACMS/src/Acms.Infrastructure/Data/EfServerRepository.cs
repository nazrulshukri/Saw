using Acms.Core.Abstractions;
using Acms.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Acms.Infrastructure.Data;

public sealed class EfServerRepository : IServerRepository
{
    private readonly AcmsDbContext _db;

    public EfServerRepository(AcmsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AwacsServer>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken = default)
    {
        var query = _db.Servers.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(s => s.IsActive);
        }

        return await query.OrderBy(s => s.Name).ToListAsync(cancellationToken);
    }

    public Task<AwacsServer?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        return _db.Servers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken cancellationToken = default)
    {
        return _db.Servers.AnyAsync(
            s => s.Name == name && (excludeId == null || s.Id != excludeId),
            cancellationToken);
    }

    public async Task AddAsync(AwacsServer server, CancellationToken cancellationToken = default)
    {
        _db.Servers.Add(server);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AwacsServer server, CancellationToken cancellationToken = default)
    {
        if (_db.Entry(server).State == EntityState.Detached)
        {
            _db.Servers.Update(server);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}

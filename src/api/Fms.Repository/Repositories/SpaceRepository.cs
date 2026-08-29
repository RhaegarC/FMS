using Fms.Interface.Repository;
using Fms.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fms.Repository.Repositories;

/// <summary>Data access for the <c>spaces</c> table.</summary>
public sealed class SpaceRepository(FmsDbContext db) : ISpaceRepository
{
    public Task<List<Space>> ListAllOrderedAsync(CancellationToken cancellationToken = default)
        => db.Spaces.AsNoTracking().OrderBy(s => s.Name).ToListAsync(cancellationToken);

    // Tracked so mutations (update/delete) persist on SaveChanges.
    public Task<Space?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => db.Spaces.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
        => db.Spaces.AsNoTracking().AnyAsync(s => s.Id == id, cancellationToken);

    public void Add(Space space) => db.Spaces.Add(space);

    public void Remove(Space space) => db.Spaces.Remove(space);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => db.SaveChangesAsync(cancellationToken);
}

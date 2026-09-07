using Fms.Interface.Repository;
using Fms.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fms.Repository.Repositories;

/// <summary>Data access for the <c>spaces</c> table.</summary>
public sealed class SpaceRepository(FmsDbContext db) : ISpaceRepository
{
    public Task<List<Space>> ListAllOrderedAsync(CancellationToken cancellationToken = default)
        => db.Spaces.AsNoTracking().OrderBy(s => s.Name).ToListAsync(cancellationToken);

    // Tracked so mutations (update/delete) persist on SaveChanges. Ids are uuid strings;
    // a malformed id is treated as not-found rather than a uuid-parameter conversion error.
    public Task<Space?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => Guid.TryParse(id, out _)
            ? db.Spaces.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            : Task.FromResult<Space?>(null);

    public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
        => Guid.TryParse(id, out _)
            ? db.Spaces.AsNoTracking().AnyAsync(s => s.Id == id, cancellationToken)
            : Task.FromResult(false);

    public void Add(Space space) => db.Spaces.Add(space);

    public void Remove(Space space) => db.Spaces.Remove(space);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => db.SaveChangesAsync(cancellationToken);
}

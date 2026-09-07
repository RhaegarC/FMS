using Fms.Interface.Repository;
using Fms.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fms.Repository.Repositories;

/// <summary>Data access for the <c>forms</c> table.</summary>
public sealed class FormRepository(FmsDbContext db) : IFormRepository
{
    public Task<List<Form>> ListBySpaceOrderedAsync(int spaceId, CancellationToken cancellationToken = default)
        => db.Forms.AsNoTracking()
            .Where(f => f.SpaceId == spaceId)
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);

    public Task<List<Form>> ListAllAsync(CancellationToken cancellationToken = default)
        => db.Forms.AsNoTracking().ToListAsync(cancellationToken);

    // Tracked so mutations (update/delete) persist on SaveChanges.
    public Task<Form?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => db.Forms.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public void Add(Form form) => db.Forms.Add(form);

    public void Remove(Form form) => db.Forms.Remove(form);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => db.SaveChangesAsync(cancellationToken);
}

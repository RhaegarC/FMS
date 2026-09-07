using Fms.Interface.Repository;
using Fms.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fms.Repository.Repositories;

/// <summary>Data access for the <c>permissions</c> table (feature 05).</summary>
public sealed class PermissionRepository(FmsDbContext db) : IPermissionRepository
{
    public Task<List<Permission>> ListAllAsync(CancellationToken cancellationToken = default)
        => db.Permissions.AsNoTracking().ToListAsync(cancellationToken);
}

using Fms.Interface.Repository;
using Fms.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fms.Repository.Repositories;

/// <summary>Data access for the <c>users</c> table (feature 03 provisions).</summary>
public sealed class UserRepository(FmsDbContext db) : IUserRepository
{
    public Task<User?> GetByEntraObjectIdAsync(string entraObjectId, CancellationToken cancellationToken = default)
        => db.Users.FirstOrDefaultAsync(u => u.EntraObjectId == entraObjectId, cancellationToken);

    public void Add(User user) => db.Users.Add(user);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => db.SaveChangesAsync(cancellationToken);
}

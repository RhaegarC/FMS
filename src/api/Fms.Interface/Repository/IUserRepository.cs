using Fms.Model.Entities;

namespace Fms.Interface.Repository;

/// <summary>Data access for the <c>users</c> table (provisioned by feature 03).</summary>
public interface IUserRepository
{
    Task<User?> GetByEntraObjectIdAsync(string entraObjectId, CancellationToken cancellationToken = default);

    void Add(User user);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

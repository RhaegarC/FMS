using Fms.Model.Entities;

namespace Fms.Interface.Repository;

/// <summary>Data access for the <c>spaces</c> table.</summary>
public interface ISpaceRepository
{
    Task<List<Space>> ListAllOrderedAsync(CancellationToken cancellationToken = default);

    Task<Space?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);

    void Add(Space space);

    void Remove(Space space);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

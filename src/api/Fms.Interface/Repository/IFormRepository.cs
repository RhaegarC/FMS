using Fms.Model.Entities;

namespace Fms.Interface.Repository;

/// <summary>Data access for the <c>forms</c> table.</summary>
public interface IFormRepository
{
    Task<List<Form>> ListBySpaceOrderedAsync(int spaceId, CancellationToken cancellationToken = default);

    Task<List<Form>> ListAllAsync(CancellationToken cancellationToken = default);

    Task<Form?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    void Add(Form form);

    void Remove(Form form);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

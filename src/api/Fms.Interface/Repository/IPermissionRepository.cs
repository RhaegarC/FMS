using Fms.Model.Entities;

namespace Fms.Interface.Repository;

/// <summary>Data access for the <c>permissions</c> table (feature 05).</summary>
public interface IPermissionRepository
{
    Task<List<Permission>> ListAllAsync(CancellationToken cancellationToken = default);
}

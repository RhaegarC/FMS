using Fms.Model.Entities;

namespace Fms.Interface.Service;

/// <summary>Space/form catalog use cases (feature 06): admin mutations, permission-scoped
/// reads for non-admins, JSON Schema validation on form create/update.</summary>
public interface ICatalogService
{
    Task<List<Space>> ListSpacesAsync(User user, CancellationToken cancellationToken = default);

    Task<Space> CreateSpaceAsync(string name, CancellationToken cancellationToken = default);

    Task<Space> UpdateSpaceAsync(string id, string name, CancellationToken cancellationToken = default);

    Task DeleteSpaceAsync(string id, CancellationToken cancellationToken = default);

    Task<List<Form>> ListFormsInSpaceAsync(string spaceId, User user, CancellationToken cancellationToken = default);

    Task<Form> CreateFormAsync(string spaceId, string name, string schema, CancellationToken cancellationToken = default);

    Task<Form> UpdateFormAsync(string id, string name, string schema, CancellationToken cancellationToken = default);

    Task DeleteFormAsync(string id, CancellationToken cancellationToken = default);
}

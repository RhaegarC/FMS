using Fms.Model.Entities;

namespace Fms.Interface.Service;

/// <summary>Space/form catalog use cases (feature 06): admin mutations, permission-scoped
/// reads for non-admins, JSON Schema validation on form create/update.</summary>
public interface ICatalogService
{
    Task<List<Space>> ListSpacesAsync(User user, CancellationToken cancellationToken = default);

    Task<Space> CreateSpaceAsync(string name, CancellationToken cancellationToken = default);

    Task<Space> UpdateSpaceAsync(int id, string name, CancellationToken cancellationToken = default);

    Task DeleteSpaceAsync(int id, CancellationToken cancellationToken = default);

    Task<List<Form>> ListFormsInSpaceAsync(int spaceId, User user, CancellationToken cancellationToken = default);

    Task<Form> CreateFormAsync(int spaceId, string name, string schema, CancellationToken cancellationToken = default);

    Task<Form> UpdateFormAsync(int id, string name, string schema, CancellationToken cancellationToken = default);

    Task DeleteFormAsync(int id, CancellationToken cancellationToken = default);
}

using Fms.Interface.Repository;
using Fms.Model.Entities;

namespace Fms.Interface.Service;

/// <summary>Submission use cases (feature 07): schema-validated submit, own-vs-all listing
/// scoped by role and the feature-05 evaluator, and the shared query contract for export.</summary>
public interface ISubmissionService
{
    Task<Submission> SubmitAsync(int formId, User user, string data, CancellationToken cancellationToken = default);

    Task<List<Submission>> ListMyAsync(User user, SubmissionQuery query, CancellationToken cancellationToken = default);

    Task<List<Submission>> ListAllAsync(SubmissionQuery query, CancellationToken cancellationToken = default);
}

using Fms.Model.Entities;

namespace Fms.Interface.Repository;

/// <summary>Data access for the <c>submissions</c> table.</summary>
public interface ISubmissionRepository
{
    /// <summary>Runs the scoped submission query (keyword/form/date filters, caller scope,
    /// accessible-form restriction) with the submitting user's row included.</summary>
    Task<List<Submission>> QueryAsync(SubmissionQuery query, CancellationToken cancellationToken = default);

    void Add(Submission submission);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

using Fms.Interface.Repository;
using Fms.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fms.Repository.Repositories;

/// <summary>Data access for the <c>submissions</c> table.</summary>
public sealed class SubmissionRepository(FmsDbContext db) : ISubmissionRepository
{
    /// <summary>Runs the scoped submission query. Keyword search matches the jsonb text
    /// representation — jsonb has no LIKE/ILIKE operator, so a parameterized raw-SQL
    /// base with an explicit <c>data::text</c> cast is used; the remaining filters
    /// (form id, date bounds, caller, accessible-form restriction) compose as LINQ.</summary>
    public async Task<List<Submission>> QueryAsync(SubmissionQuery query, CancellationToken cancellationToken = default)
    {
        IQueryable<Submission> baseQuery = string.IsNullOrWhiteSpace(query.Keyword)
            ? db.Submissions.AsNoTracking()
            : db.Submissions.FromSqlInterpolated(
                $"SELECT * FROM submissions WHERE data::text ILIKE {"%" + query.Keyword + "%"}")
            .AsNoTracking();

        if (query.FormId is not null)
        {
            baseQuery = baseQuery.Where(s => s.FormId == query.FormId);
        }

        if (query.From is not null)
        {
            baseQuery = baseQuery.Where(s => s.CreatedOn >= query.From);
        }

        if (query.To is not null)
        {
            baseQuery = baseQuery.Where(s => s.CreatedOn < query.To);
        }

        if (query.UserId is not null)
        {
            baseQuery = baseQuery.Where(s => s.UserId == query.UserId);
        }

        if (query.AccessibleFormIds is not null)
        {
            baseQuery = baseQuery.Where(s => query.AccessibleFormIds.Contains(s.FormId));
        }

        return await baseQuery
            .Include(s => s.User)
            .OrderByDescending(s => s.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public void Add(Submission submission) => db.Submissions.Add(submission);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => db.SaveChangesAsync(cancellationToken);
}

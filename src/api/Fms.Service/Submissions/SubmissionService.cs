using Fms.Interface.Repository;
using Fms.Interface.Service;
using Fms.Model.Entities;
using Fms.Service.Exceptions;
using Fms.Service.AccessControl;

namespace Fms.Service.Submissions;

/// <summary>Submission use cases (feature 07): schema-validated submit, own-vs-all
/// listing scoped by role and the feature-05 evaluator. Expresses rules as domain
/// exceptions; the Api layer maps them to HTTP status codes.</summary>
public sealed class SubmissionService(
    ISubmissionRepository submissions,
    IFormRepository forms,
    IPermissionRepository permissions,
    IPermissionEvaluator evaluator,
    IJsonSchemaValidator schemaValidator) : ISubmissionService
{
    public async Task<Submission> SubmitAsync(string formId, User user, string data, CancellationToken cancellationToken = default)
    {
        var form = await forms.GetByIdAsync(formId, cancellationToken)
            ?? throw new NotFoundException($"Form {formId} does not exist.");

        // Access-control boundary: admins bypass grants; everyone else needs a
        // space/form grant covering this form (default deny).
        if (!IsAdmin(user))
        {
            var grants = await permissions.ListAllAsync(cancellationToken);
            var subject = PermissionEvaluator.ToSubject(user);
            if (!evaluator.CanAccessForm(subject, grants, form.Id, form.SpaceId))
            {
                throw new ForbiddenException("You do not have access to this form.");
            }
        }

        if (!schemaValidator.ValidateInstance(form.Schema, data, out var validationError))
        {
            throw new ValidationException(validationError ?? "Submission does not match the form schema.");
        }

        var submission = new Submission
        {
            FormId = form.Id,
            UserId = user.Id,
            Data = data,
        };
        submissions.Add(submission);
        await submissions.SaveChangesAsync(cancellationToken);
        return submission;
    }

    /// <summary>The caller's own submissions; non-admins are further limited to forms
    /// they can access (union of space + form grants, default deny).</summary>
    public async Task<List<Submission>> ListMyAsync(User user, SubmissionQuery query, CancellationToken cancellationToken = default)
    {
        var scoped = query with { UserId = user.Id };
        if (!IsAdmin(user))
        {
            scoped = scoped with { AccessibleFormIds = await AccessibleFormIdsAsync(user, cancellationToken) };
        }

        return await submissions.QueryAsync(scoped, cancellationToken);
    }

    /// <summary>Every submission (admin only — the caller's authorization is enforced
    /// by the <c>AdminOnly</c> policy in the Api layer).</summary>
    public Task<List<Submission>> ListAllAsync(SubmissionQuery query, CancellationToken cancellationToken = default)
        => submissions.QueryAsync(query, cancellationToken);

    // Ids of forms the caller can access. Permission evaluation is in-memory (the
    // feature-05 expression grammar is the security boundary), so the candidate set
    // is loaded then filtered — same approach as the space/form list endpoints.
    private async Task<HashSet<string>> AccessibleFormIdsAsync(User user, CancellationToken cancellationToken)
    {
        var grants = await permissions.ListAllAsync(cancellationToken);
        var subject = PermissionEvaluator.ToSubject(user);
        var allForms = await forms.ListAllAsync(cancellationToken);
        return allForms
            .Where(f => evaluator.CanAccessForm(subject, grants, f.Id, f.SpaceId))
            .Select(f => f.Id)
            .ToHashSet();
    }

    private static bool IsAdmin(User user) => string.Equals(user.Role, "admin", StringComparison.OrdinalIgnoreCase);
}

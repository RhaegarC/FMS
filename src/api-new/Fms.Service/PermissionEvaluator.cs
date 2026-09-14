namespace Fms.Service;

using Fms.Interface.Service;
using Fms.Model.DatabaseEntity;

/// <summary>
/// Computes effective access from the <c>permissions</c> table (feature 03).
/// A space grant covers every form under that space (subtree); form access is the
/// union of form-level and space-level grants; resource id <c>"*"</c> matches every
/// resource of its type. Default deny: a row grants only when its expression
/// evaluates true, and an unparseable expression is treated as deny (fail closed).
/// Stateless — registered as a singleton in DI.
/// </summary>
public sealed class PermissionEvaluator : IPermissionEvaluator
{
    /// <summary>Projects a persisted <see cref="User"/> into the attribute set the
    /// expression grammar may read.</summary>
    public static PermissionSubject ToSubject(User user) => new(user.Id, user.Email, user.Role);

    /// <summary>True iff any space-level permission matching <paramref name="spaceId"/>
    /// (or <c>"*"</c>) evaluates true for <paramref name="subject"/>. Resource ids are
    /// uuid strings; comparison is case-insensitive (uuid casing is not significant).</summary>
    public bool CanAccessSpace(PermissionSubject subject, IEnumerable<Permission> permissions, string spaceId) =>
        permissions.Any(p => MatchesSpace(p, spaceId) && EvaluatesTrue(p.Expression, subject));

    /// <summary>True iff any form-level permission for <paramref name="formId"/>
    /// (or <c>"*"</c>) OR any space-level permission for <paramref name="spaceId"/>
    /// (or <c>"*"</c>) evaluates true for <paramref name="subject"/>.</summary>
    public bool CanAccessForm(
        PermissionSubject subject, IEnumerable<Permission> permissions, string formId, string spaceId) =>
        permissions.Any(p =>
            (MatchesForm(p, formId) || MatchesSpace(p, spaceId)) && EvaluatesTrue(p.Expression, subject));

    private static bool MatchesSpace(Permission p, string spaceId) =>
        IsType(p, "space") && (p.ResourceId == "*"
            || string.Equals(p.ResourceId, spaceId, StringComparison.OrdinalIgnoreCase));

    private static bool MatchesForm(Permission p, string formId) =>
        IsType(p, "form") && (p.ResourceId == "*"
            || string.Equals(p.ResourceId, formId, StringComparison.OrdinalIgnoreCase));

    private static bool IsType(Permission p, string type) =>
        string.Equals(p.ResourceType, type, StringComparison.OrdinalIgnoreCase);

    /// <summary>Evaluates an expression, treating a malformed expression as deny.</summary>
    private static bool EvaluatesTrue(string expression, PermissionSubject subject)
    {
        try
        {
            return PermissionExpression.Evaluate(expression, subject);
        }
        catch (PermissionExpressionException)
        {
            return false;
        }
    }
}

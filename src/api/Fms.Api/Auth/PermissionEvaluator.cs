using Fms.Api.Data.Entities;

namespace Fms.Api.Auth;

/// <summary>
/// Computes effective access from the <c>permissions</c> table (feature 05).
/// A space grant covers every form under that space (subtree); form access is the
/// union of form-level and space-level grants; resource id <c>"*"</c> matches every
/// resource of its type. Default deny: a row grants only when its expression
/// evaluates true, and an unparseable expression is treated as deny (fail closed).
/// </summary>
public sealed class PermissionEvaluator
{
    /// <summary>True iff any space-level permission matching <paramref name="spaceId"/>
    /// (or <c>"*"</c>) evaluates true for <paramref name="subject"/>.</summary>
    public bool CanAccessSpace(PermissionSubject subject, IEnumerable<Permission> permissions, int spaceId) =>
        permissions.Any(p => MatchesSpace(p, spaceId) && EvaluatesTrue(p.Expression, subject));

    /// <summary>True iff any form-level permission for <paramref name="formId"/>
    /// (or <c>"*"</c>) OR any space-level permission for <paramref name="spaceId"/>
    /// (or <c>"*"</c>) evaluates true for <paramref name="subject"/>.</summary>
    public bool CanAccessForm(
        PermissionSubject subject, IEnumerable<Permission> permissions, int formId, int spaceId) =>
        permissions.Any(p =>
            (MatchesForm(p, formId) || MatchesSpace(p, spaceId)) && EvaluatesTrue(p.Expression, subject));

    private static bool MatchesSpace(Permission p, int spaceId) =>
        IsType(p, "space") && (p.ResourceId == "*" || p.ResourceId == spaceId.ToString());

    private static bool MatchesForm(Permission p, int formId) =>
        IsType(p, "form") && (p.ResourceId == "*" || p.ResourceId == formId.ToString());

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

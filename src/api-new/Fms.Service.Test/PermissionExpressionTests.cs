namespace Fms.Service.Test;

using Fms.Model.DatabaseEntity;
using Fms.Service;

/// <summary>
/// RED for feature 03 (permission evaluation): the expression predicate parser and
/// evaluator. Expressions reference only user.id, user.email, user.role — anything else
/// must be rejected (the security boundary is enforced at parse time, not evaluation).
/// </summary>
public class PermissionExpressionTests
{
    // A uuid id is a string; expressions compare it as a quoted literal. (Contains a
    // hex letter so case-insensitivity of the comparison is actually exercised.)
    private const string UserId = "a0000000-0000-0000-0000-000000000007";

    /// <summary>Named <c>Subject</c> rather than the <c>User</c> it is built from: in this
    /// solution <c>User</c> is the persisted entity, and a helper of that name returning the
    /// three attributes an expression may read reads as though it were building one.</summary>
    private static PermissionSubject Subject(
        string id = UserId, string email = "alice@contoso.com", string role = "user") =>
        new(id, email, role);

    [Theory]
    [InlineData("user.role = 'admin'", "admin")]
    [InlineData("user.email = 'alice@contoso.com'", "alice@contoso.com")]
    public void Equality_MatchesExactValue(string expr, string expect)
    {
        var subject = Subject(role: "admin", email: expect);
        Assert.True(PermissionExpression.Evaluate(expr, subject));
    }

    [Fact]
    public void Equality_IsCaseSensitiveLikeSql()
    {
        var subject = Subject(email: "bob@contoso.com");
        Assert.False(PermissionExpression.Evaluate("user.email = 'BOB@contoso.com'", subject));
        Assert.False(PermissionExpression.Evaluate("user.role = 'ADMIN'", Subject(role: "admin")));
    }

    [Theory]
    [InlineData("user.role = 'admin'", "user")]
    [InlineData("user.email = 'bob@contoso.com'", "alice@contoso.com")]
    public void Equality_DoesNotMatchOtherValue(string expr, string actual)
    {
        var subject = Subject(role: actual, email: actual);
        Assert.False(PermissionExpression.Evaluate(expr, subject));
    }

    [Theory]
    [InlineData("user.role <> 'admin'", "user")]
    [InlineData("user.role != 'admin'", "user")]
    public void Inequality_MatchesDifferentValue(string expr, string actual)
    {
        var subject = Subject(role: actual);
        Assert.True(PermissionExpression.Evaluate(expr, subject));
    }

    [Fact]
    public void Equality_OnUserId_MatchesUuidString()
    {
        var subject = Subject();
        Assert.True(PermissionExpression.Evaluate($"user.id = '{UserId}'", subject));
        Assert.False(PermissionExpression.Evaluate(
            "user.id = '00000000-0000-0000-0000-000000000099'", subject));
        // uuid casing is not significant, like the DB uuid type.
        Assert.True(PermissionExpression.Evaluate(
            "user.id = 'A0000000-0000-0000-0000-000000000007'", subject));
    }

    [Fact]
    public void Like_MatchesSubstringWildcards()
    {
        var subject = Subject(email: "alice.smith@contoso.com");
        Assert.True(PermissionExpression.Evaluate("user.email LIKE '%@contoso.com'", subject));
        Assert.False(PermissionExpression.Evaluate("user.email LIKE '%@gmail.com'", subject));
    }

    [Fact]
    public void In_MatchesAnyListValue()
    {
        var subject = Subject(role: "editor");
        Assert.True(PermissionExpression.Evaluate("user.role IN ('admin', 'editor')", subject));
        Assert.False(PermissionExpression.Evaluate("user.role IN ('admin')", subject));
    }

    [Fact]
    public void And_RequiresBothOperandsTrue()
    {
        var subject = Subject(role: "admin", email: "alice@contoso.com");
        Assert.True(
            PermissionExpression.Evaluate(
                "user.role = 'admin' AND user.email = 'alice@contoso.com'", subject));
        Assert.False(
            PermissionExpression.Evaluate(
                "user.role = 'admin' AND user.email = 'bob@contoso.com'", subject));
    }

    [Fact]
    public void Or_TrueWhenAnyOperandTrue()
    {
        var subject = Subject(role: "admin", email: "alice@contoso.com");
        Assert.True(
            PermissionExpression.Evaluate(
                "user.role = 'admin' OR user.role = 'editor'", subject));
    }

    [Fact]
    public void Not_FlipsResult()
    {
        var subject = Subject(role: "user");
        Assert.True(PermissionExpression.Evaluate("NOT user.role = 'admin'", subject));
        Assert.False(PermissionExpression.Evaluate("NOT user.role = 'user'", subject));
    }

    [Fact]
    public void Parentheses_GroupExpressions()
    {
        var subject = Subject(role: "user", email: "alice@contoso.com");
        // Without parens this parses as role='user' AND email='x' OR role='admin'
        Assert.True(
            PermissionExpression.Evaluate(
                "user.role = 'user' AND (user.email = 'alice@contoso.com' OR user.role = 'admin')",
                subject));
    }

    [Theory]
    [InlineData("user.group = 'sales'")]
    [InlineData("user.name = 'Alice'")]
    [InlineData("1 = 1")]
    public void UnknownAttribute_IsRejected(string expr)
    {
        var subject = Subject();
        Assert.Throws<PermissionExpressionException>(() =>
            PermissionExpression.Evaluate(expr, subject));
    }

    [Theory]
    [InlineData("user.role = ")]
    [InlineData("user.email = 'unterminated")]
    [InlineData("user.role = 'admin' AND")]
    [InlineData("")]
    public void MalformedExpression_Throws(string expr)
    {
        var subject = Subject();
        Assert.Throws<PermissionExpressionException>(() =>
            PermissionExpression.Evaluate(expr, subject));
    }

    [Fact]
    public void NullExpression_ThrowsInsteadOfCrashing()
    {
        // Security boundary: a null expression must fail closed (deny), not NRE.
        var subject = Subject();
        Assert.Throws<PermissionExpressionException>(() =>
            PermissionExpression.Evaluate(null!, subject));
    }
}

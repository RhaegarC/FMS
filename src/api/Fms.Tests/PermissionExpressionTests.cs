using Fms.Api.Auth;

namespace Fms.Tests;

/// <summary>
/// RED for feature 05 (permissions): the expression predicate parser/evaluator.
/// Expressions reference only user.id, user.email, user.role — anything else must
/// be rejected (the security boundary is enforced at parse time, not evaluation).
/// </summary>
public class PermissionExpressionTests
{
    private static PermissionSubject User(
        int id = 7, string email = "alice@contoso.com", string role = "user") =>
        new(id, email, role);

    [Theory]
    [InlineData("user.role = 'admin'", "admin")]
    [InlineData("user.email = 'alice@contoso.com'", "alice@contoso.com")]
    public void Equality_MatchesExactValue(string expr, string expect)
    {
        var subject = User(role: "admin", email: expect);
        Assert.True(PermissionExpression.Evaluate(expr, subject));
    }

    [Fact]
    public void Equality_IsCaseSensitiveLikeSql()
    {
        var subject = User(email: "bob@contoso.com");
        Assert.False(PermissionExpression.Evaluate("user.email = 'BOB@contoso.com'", subject));
        Assert.False(PermissionExpression.Evaluate("user.role = 'ADMIN'", User(role: "admin")));
    }

    [Theory]
    [InlineData("user.role = 'admin'", "user")]
    [InlineData("user.email = 'bob@contoso.com'", "alice@contoso.com")]
    public void Equality_DoesNotMatchOtherValue(string expr, string actual)
    {
        var subject = User(role: actual, email: actual);
        Assert.False(PermissionExpression.Evaluate(expr, subject));
    }

    [Theory]
    [InlineData("user.role <> 'admin'", "user")]
    [InlineData("user.role != 'admin'", "user")]
    public void Inequality_MatchesDifferentValue(string expr, string actual)
    {
        var subject = User(role: actual);
        Assert.True(PermissionExpression.Evaluate(expr, subject));
    }

    [Fact]
    public void Equality_OnUserId_MatchesNumber()
    {
        Assert.True(PermissionExpression.Evaluate("user.id = 7", User(id: 7)));
        Assert.False(PermissionExpression.Evaluate("user.id = 99", User(id: 7)));
    }

    [Fact]
    public void Like_MatchesSubstringWildcards()
    {
        var subject = User(email: "alice.smith@contoso.com");
        Assert.True(PermissionExpression.Evaluate("user.email LIKE '%@contoso.com'", subject));
        Assert.False(PermissionExpression.Evaluate("user.email LIKE '%@gmail.com'", subject));
    }

    [Fact]
    public void In_MatchesAnyListValue()
    {
        var subject = User(role: "editor");
        Assert.True(PermissionExpression.Evaluate("user.role IN ('admin', 'editor')", subject));
        Assert.False(PermissionExpression.Evaluate("user.role IN ('admin')", subject));
    }

    [Fact]
    public void And_RequiresBothOperandsTrue()
    {
        var subject = User(role: "admin", email: "alice@contoso.com");
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
        var subject = User(role: "admin", email: "alice@contoso.com");
        Assert.True(
            PermissionExpression.Evaluate(
                "user.role = 'admin' OR user.role = 'editor'", subject));
    }

    [Fact]
    public void Not_FlipsResult()
    {
        var subject = User(role: "user");
        Assert.True(PermissionExpression.Evaluate("NOT user.role = 'admin'", subject));
        Assert.False(PermissionExpression.Evaluate("NOT user.role = 'user'", subject));
    }

    [Fact]
    public void Parentheses_GroupExpressions()
    {
        var subject = User(role: "user", email: "alice@contoso.com");
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
        var subject = User();
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
        var subject = User();
        Assert.Throws<PermissionExpressionException>(() =>
            PermissionExpression.Evaluate(expr, subject));
    }

    [Fact]
    public void NullExpression_ThrowsInsteadOfCrashing()
    {
        // Security boundary: a null expression must fail closed (deny), not NRE.
        var subject = User();
        Assert.Throws<PermissionExpressionException>(() =>
            PermissionExpression.Evaluate(null!, subject));
    }
}

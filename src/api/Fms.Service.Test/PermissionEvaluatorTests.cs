using Fms.Service.AccessControl;
using Fms.Model.Entities;

namespace Fms.Service.Test;

/// <summary>
/// RED for feature 05 (permissions): effective-access computation over `permissions`
/// rows. Space grants cover the whole subtree of forms; form access is the union of
/// form-level and space-level grants; resourceId "*" matches every resource of the
/// type; default deny when no matching row's expression is true. Resource ids are
/// uuid strings (permissions.resourceId is text: a uuid or "*").
/// </summary>
public class PermissionEvaluatorTests
{
    // Deterministic uuid strings derived from the old numeric ids: Id(1) was "space 1".
    private static string Uuid(int n) => $"00000000-0000-0000-0000-{n:D12}";

    private static readonly PermissionSubject Admin = new(Uuid(7), "alice@contoso.com", "admin");
    private static readonly PermissionSubject Bob = new(Uuid(9), "bob@contoso.com", "user");

    private static Permission P(string type, string resourceId, string expr) =>
        new() { ResourceType = type, ResourceId = resourceId, Expression = expr };

    [Fact]
    public void CanAccessSpace_TrueWhenSpaceGrantExpressionTrue()
    {
        var permissions = new[] { P("space", Uuid(1), "user.role = 'admin'") };
        Assert.True(new PermissionEvaluator().CanAccessSpace(Admin, permissions, spaceId: Uuid(1)));
    }

    [Fact]
    public void CanAccessSpace_FalseWhenExpressionFalse()
    {
        var permissions = new[] { P("space", Uuid(1), "user.role = 'admin'") };
        Assert.False(new PermissionEvaluator().CanAccessSpace(Bob, permissions, spaceId: Uuid(1)));
    }

    [Fact]
    public void CanAccessSpace_DefaultDeny_WhenNoMatchingRow()
    {
        // Row targets a different space, and none targets this space.
        var permissions = new[] { P("space", Uuid(2), "user.role = 'admin'") };
        Assert.False(new PermissionEvaluator().CanAccessSpace(Admin, permissions, spaceId: Uuid(1)));
    }

    [Fact]
    public void CanAccessSpace_DefaultDeny_WhenNoRowsAtAll()
    {
        Assert.False(new PermissionEvaluator().CanAccessSpace(Admin, [], spaceId: Uuid(1)));
    }

    [Fact]
    public void CanAccessSpace_WildcardResourceId_GrantsAnySpace()
    {
        var permissions = new[] { P("space", "*", "user.role = 'admin'") };
        Assert.True(new PermissionEvaluator().CanAccessSpace(Admin, permissions, spaceId: Uuid(42)));
        Assert.False(new PermissionEvaluator().CanAccessSpace(Bob, permissions, spaceId: Uuid(42)));
    }

    [Fact]
    public void CanAccessForm_SpaceGrant_CoversSubtree()
    {
        // Space grant for form's parent space = access to every form in it.
        var permissions = new[] { P("space", Uuid(1), "user.role = 'admin'") };
        Assert.True(new PermissionEvaluator().CanAccessForm(Admin, permissions, formId: Uuid(10), spaceId: Uuid(1)));
        Assert.False(new PermissionEvaluator().CanAccessForm(Bob, permissions, formId: Uuid(10), spaceId: Uuid(1)));
    }

    [Fact]
    public void CanAccessForm_FormGrant_GrantsThatForm()
    {
        var permissions = new[] { P("form", Uuid(10), "user.email = 'alice@contoso.com'") };
        Assert.True(new PermissionEvaluator().CanAccessForm(Admin, permissions, formId: Uuid(10), spaceId: Uuid(1)));
    }

    [Fact]
    public void CanAccessForm_Union_FormOrSpaceGrant()
    {
        // Space-level grant alone is sufficient (form 10 sits in space 1).
        var spaceGrant = new[]
        {
            P("form", Uuid(10), "user.email = 'nobody@nowhere.com'"),
            P("space", Uuid(1), "user.role = 'admin'"),
        };
        Assert.True(new PermissionEvaluator().CanAccessForm(Admin, spaceGrant, formId: Uuid(10), spaceId: Uuid(1)));

        // Form-level grant alone is sufficient for a matching user.
        var carol = new PermissionSubject(Uuid(11), "carol@contoso.com", "user");
        var formGrant = new[] { P("form", Uuid(10), "user.email = 'carol@contoso.com'") };
        Assert.True(new PermissionEvaluator().CanAccessForm(carol, formGrant, formId: Uuid(10), spaceId: Uuid(1)));
    }

    [Fact]
    public void CanAccessForm_WildcardFormGrant_GrantsAnyForm()
    {
        var permissions = new[] { P("form", "*", "user.role = 'admin'") };
        Assert.True(new PermissionEvaluator().CanAccessForm(Admin, permissions, formId: Uuid(999), spaceId: Uuid(1)));
        Assert.False(new PermissionEvaluator().CanAccessForm(Bob, permissions, formId: Uuid(999), spaceId: Uuid(1)));
    }

    [Fact]
    public void CanAccessForm_DefaultDeny_WhenNeitherFormNorSpaceGrant()
    {
        var permissions = new[]
        {
            P("form", Uuid(11), "user.role = 'admin'"), // another form
            P("space", Uuid(2), "user.role = 'admin'"), // another space
        };
        Assert.False(new PermissionEvaluator().CanAccessForm(Admin, permissions, formId: Uuid(10), spaceId: Uuid(1)));
    }

    [Fact]
    public void CanAccessForm_MalformedExpression_DeniesClosed()
    {
        // A matching row whose expression can't be parsed must not grant (fail closed).
        var permissions = new[] { P("form", Uuid(10), "user.role = "), P("space", Uuid(1), "user.role = 'admin' AND") };
        Assert.False(new PermissionEvaluator().CanAccessForm(Admin, permissions, formId: Uuid(10), spaceId: Uuid(1)));
        Assert.False(new PermissionEvaluator().CanAccessForm(Bob, permissions, formId: Uuid(10), spaceId: Uuid(1)));
    }
}

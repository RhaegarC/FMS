namespace Fms.Model.Entities;

/// <summary>
/// A grant expression. <see cref="ResourceType"/> + <see cref="ResourceId"/> target the
/// resource; <see cref="Expression"/> is a SQL predicate over <c>user.id</c>/<c>email</c>/
/// <c>role</c> evaluated at request time (feature 05). A space grant covers all forms under
/// that space (<c>spaceA.*</c>); <see cref="ResourceId"/> of <c>"*"</c> matches every
/// resource of the type. Default deny when no grant matches.
/// </summary>
public class Permission : IAuditable
{
    public string Id { get; set; } = null!;

    /// <summary>Either <c>space</c> or <c>form</c>.</summary>
    public string ResourceType { get; set; } = null!;

    /// <summary>Target id as text (a uuid string), or <c>"*"</c> for all resources of the type.</summary>
    public string ResourceId { get; set; } = null!;

    public string Expression { get; set; } = null!;

    public string? CreatedBy { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset LastModifiedOn { get; set; }
}

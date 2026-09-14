namespace Fms.Model.DatabaseEntity;

/// <summary>
/// A grant expression. <see cref="ResourceType"/> + <see cref="ResourceId"/> target the
/// resource; <see cref="Expression"/> is a predicate over <c>user.id</c>/<c>user.email</c>/
/// <c>user.role</c> evaluated at request time (feature 03). A space grant covers all forms
/// under that space (<c>spaceA.*</c>); <see cref="ResourceId"/> of <c>"*"</c> matches every
/// resource of the type. Default deny when no grant matches.
/// </summary>
/// <remarks>
/// Note that <c>user.id</c> here is the Entra object id, because that is what
/// <see cref="User.Id"/> holds.
/// </remarks>
public sealed class Permission : EntityBase
{
    /// <summary>Either <c>space</c> or <c>form</c>.</summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>Target id as text, or <c>"*"</c> for all resources of the type.</summary>
    public string ResourceId { get; set; } = string.Empty;

    public string Expression { get; set; } = string.Empty;
}

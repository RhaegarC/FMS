namespace Fms.Model.DatabaseEntity;

/// <summary>
/// A platform user, authenticated via Entra ID (feature 03). No credentials stored.
/// </summary>
/// <remarks>
/// <see cref="EntityBase.Id"/> <em>is</em> the Entra object id — the same value as the
/// <c>oid</c> claim. There is no separate external-identity column: the object id is
/// stable for the life of the account, it is the only thing the token reliably carries,
/// and a second column would be a second thing to keep in step with it.
///
/// It still satisfies the uuid column contract because Entra object ids are GUIDs. Note
/// that the <c>sub</c> claim is <em>not</em> guaranteed to be one, so a token without
/// <c>oid</c> cannot be provisioned — see <c>UserService</c>.
/// </remarks>
public sealed class User : EntityBase
{
    public string Email { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Either <c>admin</c> or <c>user</c> (seeded from <c>ADMIN_USER_IDS</c>).</summary>
    public string Role { get; set; } = "user";

    public List<Submission> Submissions { get; } = [];
}

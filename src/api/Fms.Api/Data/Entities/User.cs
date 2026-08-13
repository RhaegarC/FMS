namespace Fms.Api.Data.Entities;

/// <summary>A platform user, authenticated via Entra ID (feature 03). No credentials stored.</summary>
public class User
{
    public int Id { get; set; }

    /// <summary>Entra ID object id (the <c>sub</c>/<c>oid</c> claim). Unique external identity.</summary>
    public string EntraObjectId { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Name { get; set; } = null!;

    /// <summary>Either <c>admin</c> or <c>user</c> (seeded from <c>ADMIN_USER_IDS</c>, feature 03).</summary>
    public string Role { get; set; } = "user";

    public List<Submission> Submissions { get; } = [];
}

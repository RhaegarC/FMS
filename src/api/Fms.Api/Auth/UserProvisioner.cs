using System.Security.Claims;
using Fms.Api.Data;
using Fms.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fms.Api.Auth;

/// <summary>
/// Creates-or-updates a <see cref="User"/> row from an authenticated Entra principal
/// on every request (feature 03). First login provisions role <c>user</c>; users in
/// <c>ADMIN_USER_IDS</c> (comma-separated Entra object ids, from config/env) are
/// provisioned or promoted to <c>admin</c>. No credentials are stored — the Entra
/// object id is the unique external identity.
/// </summary>
public class UserProvisioner(FmsDbContext db, IConfiguration configuration)
{
    private readonly HashSet<string> _adminUserIds = new(
        (configuration["ADMIN_USER_IDS"] ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Ensures a <see cref="User"/> exists for <paramref name="principal"/> and returns
    /// it. Promotes existing users to <c>admin</c> when they appear in
    /// <c>ADMIN_USER_IDS</c>, but never demotes.
    /// </summary>
    public async Task<User> ProvisionAsync(ClaimsPrincipal principal)
    {
        // Entra v2.0 tokens carry both `oid` and `sub`; object id is the stable identity.
        var entraObjectId = principal.FindFirst("oid")?.Value
                            ?? principal.FindFirst("sub")?.Value
                            ?? throw new InvalidOperationException(
                                "Authenticated principal has no oid/sub claim.");

        var email = principal.FindFirst("email")?.Value ?? string.Empty;
        var name = principal.FindFirst("name")?.Value ?? email;

        var user = await db.Users.FirstOrDefaultAsync(u => u.EntraObjectId == entraObjectId);
        if (user is null)
        {
            user = new User { EntraObjectId = entraObjectId, Email = email, Name = name };
            db.Users.Add(user);
        }
        else
        {
            // Entra is the source of truth for profile data — refresh on each login.
            user.Email = email;
            user.Name = name;
        }

        if (_adminUserIds.Contains(entraObjectId))
        {
            user.Role = "admin";
        }

        await db.SaveChangesAsync();
        return user;
    }
}

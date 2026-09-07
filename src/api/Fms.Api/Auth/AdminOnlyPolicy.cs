using Fms.Model.Entities;
using Microsoft.AspNetCore.Authorization;

namespace Fms.Api.Auth;

/// <summary>
/// Policy for admin-only endpoints (feature 06). The Fms role is DB-stored (not a
/// JWT claim), so the handler reads the provisioned user from HttpContext.Items
/// instead of the token. <c>UserProvisioningMiddleware</c> runs before authorization,
/// so the item is populated for every authenticated request.
/// </summary>
public sealed class AdminOnlyRequirement : IAuthorizationRequirement;

public sealed class AdminOnlyHandler : AuthorizationHandler<AdminOnlyRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, AdminOnlyRequirement requirement)
    {
        if (context.Resource is HttpContext http &&
            http.Items[UserProvisioningMiddleware.FmsUserKey] is User user &&
            string.Equals(user.Role, "admin", StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

using Fms.Interface.Service;
using Fms.Model.Entities;

namespace Fms.Api.Auth;

/// <summary>
/// Runs after authentication (feature 03): provisions the <c>users</c> row for an
/// authenticated principal and exposes it to handlers via
/// <see cref="HttpContext.Items"/> under <see cref="FmsUserKey"/>. Anonymous requests
/// pass through untouched (protected endpoints reject them at authorization).
/// Provisioning itself lives in the Service layer (<see cref="IUserProvisioner"/>).
/// </summary>
public class UserProvisioningMiddleware(RequestDelegate next)
{
    /// <summary><c>HttpContext.Items</c> key holding the provisioned <see cref="User"/>.</summary>
    public const string FmsUserKey = "FmsUser";

    public async Task InvokeAsync(HttpContext context, IUserProvisioner provisioner)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            context.Items[FmsUserKey] = await provisioner.ProvisionAsync(context.User, context.RequestAborted);
        }

        await next(context);
    }
}

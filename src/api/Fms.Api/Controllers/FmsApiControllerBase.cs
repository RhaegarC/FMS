using Fms.Api.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Fms.Api.Controllers;

/// <summary>
/// Base for all API controllers. Exposes the provisioned Fms user for the current
/// request (<see cref="UserProvisioningMiddleware"/> stores it in HttpContext.Items
/// before authorization runs). The <c>User</c> type is fully-qualified because
/// <see cref="ControllerBase.User"/> already names the ClaimsPrincipal.
/// </summary>
public abstract class FmsApiControllerBase : ControllerBase
{
    protected Fms.Model.Entities.User CurrentUser =>
        (Fms.Model.Entities.User)HttpContext.Items[UserProvisioningMiddleware.FmsUserKey]!;
}

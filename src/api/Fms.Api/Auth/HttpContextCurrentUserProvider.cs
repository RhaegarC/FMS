using Fms.Interface.Service;
using Fms.Model.Entities;
using Microsoft.AspNetCore.Http;

namespace Fms.Api.Auth;

/// <summary>
/// Api-layer implementation of <see cref="ICurrentUserProvider"/> (backend-standard
/// §6.1.1): resolves the current actor from the provisioned Fms user that
/// <see cref="UserProvisioningMiddleware"/> stores in HttpContext.Items. Null outside a
/// request or during first-login provisioning (the user's own row is system-created).
/// </summary>
public sealed class HttpContextCurrentUserProvider(IHttpContextAccessor http) : ICurrentUserProvider
{
    public string? EntraObjectId =>
        http.HttpContext?.Items[UserProvisioningMiddleware.FmsUserKey] is User user
            ? user.EntraObjectId
            : null;
}

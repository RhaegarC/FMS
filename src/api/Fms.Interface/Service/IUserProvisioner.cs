using System.Security.Claims;
using Fms.Model.Entities;

namespace Fms.Interface.Service;

/// <summary>Ensures a <c>users</c> row exists for an authenticated principal (feature 03).</summary>
public interface IUserProvisioner
{
    Task<User> ProvisionAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
}

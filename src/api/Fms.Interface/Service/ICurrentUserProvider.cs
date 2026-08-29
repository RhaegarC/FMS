namespace Fms.Interface.Service;

/// <summary>
/// Supplies the current request's actor for audit columns (backend-standard §6.1.1).
/// Implemented in the Api layer from the authenticated principal; consumed by the
/// Repository layer's SaveChanges interceptor so the two never touch HTTP directly.
/// </summary>
public interface ICurrentUserProvider
{
    /// <summary>Entra object id (AAD GUID) of the current user, or null when there is
    /// no authenticated request (e.g. background jobs, first-login provisioning).</summary>
    string? EntraObjectId { get; }
}

namespace Fms.Model.Entities;

/// <summary>
/// Common audit columns for every table (backend-standard §6.1.1): who created /
/// modified a row and when. <see cref="CreatedBy"/> and <see cref="LastModifiedBy"/>
/// store the actor's Entra object id (AAD GUID) — the same value as
/// <see cref="User.EntraObjectId"/>. Values are set automatically by the Repository
/// layer's SaveChanges interceptor; handlers never set them.
/// </summary>
public interface IAuditable
{
    /// <summary>Entra object id (AAD GUID) of the user who created the row, or null when
    /// the row is system-created (e.g. the user's own first-login provisioning).</summary>
    string? CreatedBy { get; set; }

    DateTimeOffset CreatedOn { get; set; }

    /// <summary>Entra object id (AAD GUID) of the user who last modified the row.</summary>
    string? LastModifiedBy { get; set; }

    DateTimeOffset LastModifiedOn { get; set; }
}

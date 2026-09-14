namespace Fms.Model.DatabaseEntity;

/// <summary>
/// The user attributes a permission expression may reference (feature 03).
/// This is the security boundary: expressions can only read these three fields,
/// enforced at parse time so a permissions row can never probe other data.
/// </summary>
public readonly record struct PermissionSubject(string Id, string Email, string Role);

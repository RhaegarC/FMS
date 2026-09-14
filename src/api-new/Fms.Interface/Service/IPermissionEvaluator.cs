namespace Fms.Interface.Service;

using Fms.Model.DatabaseEntity;

/// <summary>Computes effective access from the <c>permissions</c> table (feature 03).</summary>
public interface IPermissionEvaluator
{
    bool CanAccessSpace(PermissionSubject subject, IEnumerable<Permission> permissions, string spaceId);

    bool CanAccessForm(PermissionSubject subject, IEnumerable<Permission> permissions, string formId, string spaceId);
}

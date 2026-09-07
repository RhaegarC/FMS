using Fms.Model.Entities;

namespace Fms.Interface.Service;

/// <summary>Computes effective access from the <c>permissions</c> table (feature 05).</summary>
public interface IPermissionEvaluator
{
    bool CanAccessSpace(PermissionSubject subject, IEnumerable<Permission> permissions, int spaceId);

    bool CanAccessForm(PermissionSubject subject, IEnumerable<Permission> permissions, int formId, int spaceId);
}

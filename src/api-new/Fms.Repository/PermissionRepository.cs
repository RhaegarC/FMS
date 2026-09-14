namespace Fms.Repository;

using Fms.Interface.Repository;

public sealed class PermissionRepository(FmsDbContext context) : DatabaseRepository(context), IPermissionRepository
{
}

namespace Fms.Repository;

using Fms.Interface.Repository;

public sealed class UserRepository(FmsDbContext context) : DatabaseRepository(context), IUserRepository
{
}

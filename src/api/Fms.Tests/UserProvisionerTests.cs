using System.Security.Claims;
using Fms.Api.Auth;
using Fms.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Fms.Tests;

/// <summary>
/// Unit tests for the first-login user provisioning service (feature 03).
/// Uses EF Core InMemory for speed; the full stack (real Postgres + JWT bearer)
/// is covered by <see cref="AuthIntegrationTests"/>.
/// </summary>
public class UserProvisionerTests
{
    private static FmsDbContext CreateDb(string name) =>
        new(new DbContextOptionsBuilder<FmsDbContext>().UseInMemoryDatabase(name).Options);

    private static IConfiguration Config(string adminUserIds = "") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ADMIN_USER_IDS"] = adminUserIds,
            })
            .Build();

    private static ClaimsPrincipal Principal(string oid, string email = "", string name = "") =>
        new(new ClaimsIdentity(new[]
        {
            new Claim("oid", oid),
            new Claim("email", email),
            new Claim("name", name),
        }, "Test"));

    [Fact]
    public async Task ProvisionAsync_NewUser_ReturnsRoleUser()
    {
        var db = CreateDb("new-user");
        var provisioner = new UserProvisioner(db, Config());

        var user = await provisioner.ProvisionAsync(Principal("oid-1", "alice@example.com", "Alice"));

        Assert.Equal("user", user.Role);
        Assert.Equal("oid-1", user.EntraObjectId);
        Assert.Equal("alice@example.com", user.Email);
        Assert.Equal("Alice", user.Name);
        Assert.Equal(1, await db.Users.CountAsync());
    }

    [Fact]
    public async Task ProvisionAsync_UserInAdminUserIds_ReturnsRoleAdmin()
    {
        var db = CreateDb("admin-new");
        var provisioner = new UserProvisioner(db, Config("oid-1,some-other"));

        var user = await provisioner.ProvisionAsync(Principal("oid-1", "alice@example.com", "Alice"));

        Assert.Equal("admin", user.Role);
    }

    [Fact]
    public async Task ProvisionAsync_ExistingUserAddedToAdminList_PromotesToAdmin()
    {
        var db = CreateDb("promote");
        await new UserProvisioner(db, Config()).ProvisionAsync(
            Principal("oid-1", "alice@example.com", "Alice"));

        var user = await new UserProvisioner(db, Config("oid-1")).ProvisionAsync(
            Principal("oid-1", "alice@example.com", "Alice"));

        Assert.Equal("admin", user.Role);
        Assert.Equal(1, await db.Users.CountAsync());
    }

    [Fact]
    public async Task ProvisionAsync_RemovedFromAdminList_DoesNotDemote()
    {
        var db = CreateDb("no-demote");
        await new UserProvisioner(db, Config("oid-1")).ProvisionAsync(
            Principal("oid-1", "alice@example.com", "Alice"));

        var user = await new UserProvisioner(db, Config()).ProvisionAsync(
            Principal("oid-1", "alice@example.com", "Alice"));

        Assert.Equal("admin", user.Role); // never demote
    }

    [Fact]
    public async Task ProvisionAsync_ExistingUser_UpdatesEmailAndName()
    {
        var db = CreateDb("update-profile");
        var provisioner = new UserProvisioner(db, Config());
        await provisioner.ProvisionAsync(Principal("oid-1", "alice@example.com", "Alice"));

        var user = await provisioner.ProvisionAsync(
            Principal("oid-1", "alice@new.example.com", "Alicia"));

        Assert.Equal("alice@new.example.com", user.Email);
        Assert.Equal("Alicia", user.Name);
        Assert.Equal(1, await db.Users.CountAsync());
    }
}

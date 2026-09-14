namespace Fms.Service.Test;

using Fms.Model.DatabaseEntity;
using Fms.Service;
using Fms.TestSupport;

/// <summary>
/// The behaviour behind <c>GET /user/me</c>: the caller's own row comes back, and is
/// provisioned on the first call that mentions them.
/// </summary>
public class UserServiceTests
{
    /// <summary>A real GUID, because the object id is the row's primary key and that column
    /// is a uuid. A friendly id like "oid-123" would make these tests pass against a value
    /// the database would reject.</summary>
    private const string ObjectId = "8f2c1e40-5b6a-4d7e-9c31-2a4f6b8d0e15";

    [Fact]
    public async Task A_caller_with_no_entra_id_is_not_persisted()
    {
        var repository = new StubUserRepository();
        var service = new UserService(repository, Caller(entraObjectId: null));

        User? user = await service.GetOrCreateAsync();

        Assert.Null(user);
        Assert.Empty(repository.Created);
    }

    [Fact]
    public async Task An_object_id_that_is_not_a_guid_is_not_persisted()
    {
        // The id is the primary key and that column is a uuid, so a non-GUID object id
        // cannot be stored. Without the guard this throws FormatException from Guid.Parse
        // inside the provider while building the insert — a 500 for what is really
        // "this caller has no identity we can key on".
        var repository = new StubUserRepository();
        var service = new UserService(repository, Caller("not-a-guid"));

        User? user = await service.GetOrCreateAsync();

        Assert.Null(user);
        Assert.Empty(repository.Created);
    }

    [Fact]
    public async Task A_first_request_creates_a_user_keyed_by_the_entra_object_id()
    {
        var repository = new StubUserRepository();
        var service = new UserService(repository, Caller(ObjectId, "Ada Lovelace"));

        User? user = await service.GetOrCreateAsync();

        Assert.NotNull(user);
        Assert.Equal(ObjectId, user!.Id);
        Assert.Equal("Ada Lovelace", user.Name);

        // Exactly one insert, and it is the row that was returned.
        Assert.Same(user, Assert.Single(repository.Created));
    }

    [Fact]
    public async Task A_known_caller_is_returned_without_a_second_insert()
    {
        var existing = new User { Id = ObjectId, Name = "Ada Lovelace" };
        var repository = new StubUserRepository { Stored = existing };
        var service = new UserService(repository, Caller(ObjectId, "Ada Lovelace"));

        User? user = await service.GetOrCreateAsync();

        Assert.Same(existing, user);
        Assert.Empty(repository.Created);
    }

    private static FakeUserContext Caller(string? entraObjectId, string? actorName = null) =>
        new()
        {
            EntraObjectId = entraObjectId,
            ActorName = actorName,
            HasActiveRequest = true,
        };
}

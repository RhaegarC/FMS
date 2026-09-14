namespace Fms.Service;

using Fms.Interface.Infrastructure;
using Fms.Interface.Repository;
using Fms.Interface.Service;
using Fms.Model.DatabaseEntity;

public sealed class UserService(
    IUserRepository userRepository,
    IUserContextService userContext) : IUserService
{
    /// <inheritdoc/>
    public async Task<User?> GetOrCreateAsync()
    {
        string? entraObjectId = userContext.EntraObjectId;

        // A validated token still might not carry an object id. There is no identity to
        // key a row on, so there is nothing to look up and — more importantly — nothing
        // that may be inserted: an unidentifiable caller must not create a user.
        if (string.IsNullOrWhiteSpace(entraObjectId))
        {
            return null;
        }

        // The row's key *is* the object id, and that column is a uuid, so an id that is not
        // one cannot be stored at all. Refusing it here keeps the failure legible: left to
        // the provider, the value converter throws FormatException from inside Guid.Parse
        // while building the insert, which surfaces as an opaque 500 rather than as "this
        // caller has no identity we can key on".
        //
        // This is why the token's `sub` claim is not accepted as a fallback. Entra object
        // ids are GUIDs; `sub` is a pairwise identifier with no such guarantee.
        if (!Guid.TryParse(entraObjectId, out _))
        {
            return null;
        }

        User? user = await userRepository.GetAsync<User>(existing => existing.Id == entraObjectId);

        if (user is not null)
        {
            return user;
        }

        // The object id is the row's identity, so it is assigned here rather than left to
        // the constructor's generated key.
        //
        // `name` is a required column, so a token that carries no display name gets an
        // empty one rather than failing the insert. The email and name claims are not read
        // here yet — provisioning currently records identity and nothing else.
        user = new User
        {
            Id = entraObjectId,
            Name = userContext.ActorName ?? string.Empty,
        };

        await userRepository.CreateAsync(user);

        return user;
    }
}

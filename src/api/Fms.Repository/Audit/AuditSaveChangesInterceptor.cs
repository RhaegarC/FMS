using Fms.Interface.Service;
using Fms.Model.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fms.Repository.Audit;

/// <summary>
/// Single source of truth for the audit columns (backend-standard §6.1.1): stamps
/// <c>created_*</c> on insert and <c>last_modified_*</c> on insert/update for every
/// <see cref="IAuditable"/> entity. The actor comes from <see cref="ICurrentUserProvider"/>
/// (implemented in the Api layer) so the Repository never touches HTTP. Handlers never
/// set audit columns themselves.
/// </summary>
public sealed class AuditSaveChangesInterceptor(ICurrentUserProvider currentUser) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void Apply(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var actor = currentUser.EntraObjectId;

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedOn = now;
                    entry.Entity.CreatedBy ??= actor;
                    entry.Entity.LastModifiedOn = now;
                    entry.Entity.LastModifiedBy = actor;
                    break;
                case EntityState.Modified:
                    entry.Entity.LastModifiedOn = now;
                    entry.Entity.LastModifiedBy = actor;
                    break;
            }
        }
    }
}

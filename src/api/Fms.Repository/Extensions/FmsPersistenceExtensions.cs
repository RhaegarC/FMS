using Fms.Interface.Repository;
using Fms.Repository.Audit;
using Fms.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Fms.Repository.Extensions;

/// <summary>Composition-root hook for the Repository layer (backend-standard §4.3):
/// wires up the EF Core context and registers every repository by its interface.</summary>
public static class FmsPersistenceExtensions
{
    /// <summary>Registers <see cref="FmsDbContext"/> (UseNpgsql only when a connection
    /// string is present — preserves no-DB startup for /health and Swagger), the audit
    /// interceptor (§6.1.1), and the five repositories as scoped services by interface.
    /// Column names are camelCase (no underscores), mapped explicitly in the DbContext.</summary>
    public static IServiceCollection AddFmsPersistence(this IServiceCollection services, string? connectionString)
    {
        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<FmsDbContext>((serviceProvider, options) =>
        {
            if (!string.IsNullOrEmpty(connectionString))
            {
                options.UseNpgsql(connectionString)
                    .AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>());
            }
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISpaceRepository, SpaceRepository>();
        services.AddScoped<IFormRepository, FormRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<ISubmissionRepository, SubmissionRepository>();

        return services;
    }
}

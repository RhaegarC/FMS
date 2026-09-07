using Fms.Interface.Service;
using Fms.Service.Catalog;
using Fms.Service.Export;
using Fms.Service.AccessControl;
using Fms.Service.Submissions;
using Fms.Service.Users;
using Fms.Service.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Fms.Service.Extensions;

/// <summary>Composition-root hook for the Service layer (backend-standard §4.3):
/// registers every service behind its interface. Stateful services (those with
/// repository/DbContext dependencies) are scoped; stateless pure-logic services are
/// singletons.</summary>
public static class FmsServiceExtensions
{
    public static IServiceCollection AddFmsServices(this IServiceCollection services)
    {
        services.AddScoped<IUserProvisioner, UserProvisioner>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<ISubmissionService, SubmissionService>();

        services.AddSingleton<IPermissionEvaluator, PermissionEvaluator>();
        services.AddSingleton<IJsonSchemaValidator, JsonSchemaValidator>();
        services.AddSingleton<ISubmissionExcelExporter, SubmissionExcelExporter>();

        return services;
    }
}

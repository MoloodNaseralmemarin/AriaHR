using AriaHR.Modules.Organization.API.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace AriaHR.Modules.Organization.API;

public static class DependencyInjection
{
    public static IServiceCollection AddOrganizationApi(this IServiceCollection services)
    {
        services.AddControllers()
            .AddApplicationPart(typeof(OrganizationsController).Assembly);

        return services;
    }
}

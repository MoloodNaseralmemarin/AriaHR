using AriaHR.Modules.Identity.API.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace AriaHR.Modules.Identity.API;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityApi(this IServiceCollection services)
    {
        services.AddControllers()
            .AddApplicationPart(typeof(AuthController).Assembly);

        return services;
    }
}

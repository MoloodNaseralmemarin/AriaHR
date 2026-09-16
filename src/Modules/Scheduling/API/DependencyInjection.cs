using AriaHR.Modules.Scheduling.API.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace AriaHR.Modules.Scheduling.API;

public static class DependencyInjection
{
    public static IServiceCollection AddSchedulingApi(this IServiceCollection services)
    {
        services.AddControllers()
            .AddApplicationPart(typeof(ShiftsController).Assembly);

        return services;
    }
}

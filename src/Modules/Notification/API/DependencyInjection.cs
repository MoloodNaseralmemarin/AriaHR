using Microsoft.Extensions.DependencyInjection;

namespace AriaHR.Modules.Notification.API;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationApi(this IServiceCollection services)
    {
        services.AddControllers()
            .AddApplicationPart(typeof(DependencyInjection).Assembly);

        return services;
    }
}

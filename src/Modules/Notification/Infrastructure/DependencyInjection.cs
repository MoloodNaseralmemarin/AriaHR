using AriaHR.Modules.Notification.Application.Repositories;
using AriaHR.Modules.Notification.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace AriaHR.Modules.Notification.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IUserDeviceRepository, UserDeviceRepository>();

        return services;
    }
}

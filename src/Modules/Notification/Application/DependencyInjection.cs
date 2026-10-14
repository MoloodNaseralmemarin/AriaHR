using AriaHR.Modules.Notification.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AriaHR.Modules.Notification.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationApplication(this IServiceCollection services)
    {
        services.AddScoped<INotificationService, NotificationService>();
        return services;
    }
}

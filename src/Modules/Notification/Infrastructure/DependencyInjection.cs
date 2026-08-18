using AriaHR.Modules.Notification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AriaHR.Modules.Notification.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("NotificationDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'NotificationDatabase' was not configured.");
        }

        services.AddDbContext<NotificationDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        return services;
    }
}

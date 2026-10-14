using AriaHR.Modules.Notification.Application;
using AriaHR.Modules.Notification.Application.Repositories;
using AriaHR.Modules.Notification.Infrastructure.Persistence;
using AriaHR.Modules.Notification.Infrastructure.Repositories;
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
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not configured.");
        }

        services.AddDbContext<NotificationDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory_Notification");
            });
        });

        return services.AddNotificationInfrastructure();
    }

    public static IServiceCollection AddNotificationInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IUserDeviceRepository, UserDeviceRepository>();
        services.AddScoped<ICrossCenterPermissionRepository, CrossCenterPermissionRepository>();

        services.AddNotificationApplication();

        return services;
    }
}

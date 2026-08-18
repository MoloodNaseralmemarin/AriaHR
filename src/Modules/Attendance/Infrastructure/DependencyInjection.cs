using AriaHR.Modules.Attendance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AriaHR.Modules.Attendance.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAttendanceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AttendanceDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'AttendanceDatabase' was not configured.");
        }

        services.AddDbContext<AttendanceDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        return services;
    }
}

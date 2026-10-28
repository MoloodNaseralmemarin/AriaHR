using AriaHR.Modules.Attendance.API.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace AriaHR.Modules.Attendance.API;

public static class DependencyInjection
{
    public static IServiceCollection AddAttendanceApi(this IServiceCollection services)
    {
        services.AddControllers()
            .AddApplicationPart(typeof(AttendanceController).Assembly);

        return services;
    }
}

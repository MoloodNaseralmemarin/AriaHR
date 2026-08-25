using AriaHR.Modules.Scheduling.Application.Repositories;
using AriaHR.Modules.Scheduling.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AriaHR.Modules.Scheduling.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSchedulingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddSchedulingInfrastructure();
    }

    public static IServiceCollection AddSchedulingInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IShiftRepository, ShiftRepository>();
        services.AddScoped<IShiftAssignmentRepository, ShiftAssignmentRepository>();

        return services;
    }
}

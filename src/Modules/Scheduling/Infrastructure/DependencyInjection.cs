using AriaHR.Modules.Scheduling.Application.Repositories;
using AriaHR.Modules.Scheduling.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace AriaHR.Modules.Scheduling.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSchedulingInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IShiftRepository, ShiftRepository>();
        services.AddScoped<IShiftAssignmentRepository, ShiftAssignmentRepository>();

        return services;
    }
}

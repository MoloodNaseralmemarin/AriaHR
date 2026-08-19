using AriaHR.Modules.Requests.Application.Repositories;
using AriaHR.Modules.Requests.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace AriaHR.Modules.Requests.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRequestsInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();
        services.AddScoped<IMissionRequestRepository, MissionRequestRepository>();
        services.AddScoped<ILeaveTypeRepository, LeaveTypeRepository>();
        services.AddScoped<ILeaveBalanceRepository, LeaveBalanceRepository>();

        return services;
    }
}

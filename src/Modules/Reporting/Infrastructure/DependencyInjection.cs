using AriaHR.Modules.Reporting.Application.Repositories;
using AriaHR.Modules.Reporting.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace AriaHR.Modules.Reporting.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddReportingInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IReportingQueryRepository, ReportingQueryRepository>();

        return services;
    }
}

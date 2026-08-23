using AriaHR.Modules.Payroll.Application.Repositories;
using AriaHR.Modules.Payroll.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace AriaHR.Modules.Payroll.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPayrollInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IPayrollRepository, PayrollRepository>();

        return services;
    }
}

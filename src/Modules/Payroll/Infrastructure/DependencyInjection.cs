using AriaHR.Modules.Payroll.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AriaHR.Modules.Payroll.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPayrollModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PayrollDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'PayrollDatabase' was not configured.");
        }

        services.AddDbContext<PayrollDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        return services;
    }
}

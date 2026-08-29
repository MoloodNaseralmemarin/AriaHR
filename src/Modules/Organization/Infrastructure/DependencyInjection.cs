using AriaHR.Modules.Organization.Application.Repositories;
using AriaHR.Modules.Organization.Application.UseCases.CreateOrganization;
using AriaHR.Modules.Organization.Application.UseCases.GetDashboardSummary;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using AriaHR.Modules.Organization.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AriaHR.Modules.Organization.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrganizationModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not configured.");
        }

        services.AddDbContext<OrganizationDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory_Organization");
            });
        });

        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<ICreateOrganizationUseCase, CreateOrganizationUseCase>();
        services.AddScoped<IGetOrganizationsDashboardSummaryUseCase, GetOrganizationsDashboardSummaryUseCase>();

        return services;
    }
}

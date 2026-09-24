using AriaHR.Modules.Organization.Application.Repositories;
using AriaHR.Modules.Organization.Application.Services;
using AriaHR.Modules.Organization.Application.UseCases.CreateEmployee;
using AriaHR.Modules.Organization.Application.UseCases.CreateOrganization;
using AriaHR.Modules.Organization.Application.Options;
using AriaHR.Modules.Organization.Application.UseCases.CreateWorkLocation;
using AriaHR.Modules.Organization.Application.UseCases.GenerateQrCode;
using AriaHR.Modules.Organization.Application.UseCases.GetDashboardSummary;
using AriaHR.Modules.Organization.Application.UseCases.GetRecentActivities;
using AriaHR.Modules.Organization.Application.UseCases.GetRecentOrganizations;
using AriaHR.Modules.Organization.Application.UseCases.GetTotalOrganizationsCount;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using AriaHR.Modules.Organization.Infrastructure.Repositories;
using AriaHR.Modules.Organization.Infrastructure.Services;
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

        services.Configure<QrCodeOptions>(configuration.GetSection(QrCodeOptions.SectionName));

        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IWorkLocationRepository, WorkLocationRepository>();
        services.AddScoped<IQrCodeRepository, QrCodeRepository>();
        services.AddScoped<IOrganizationManagerIdentityService, OrganizationManagerIdentityService>();
        services.AddScoped<ICreateOrganizationUseCase, CreateOrganizationUseCase>();
        services.AddScoped<ICreateEmployeeUseCase, CreateEmployeeUseCase>();
        services.AddScoped<ICreateWorkLocationUseCase, CreateWorkLocationUseCase>();
        services.AddScoped<IGenerateQrCodeUseCase, GenerateQrCodeUseCase>();
        services.AddScoped<IGetOrganizationsDashboardSummaryUseCase, GetOrganizationsDashboardSummaryUseCase>();
        services.AddScoped<IGetTotalOrganizationsCountUseCase, GetTotalOrganizationsCountUseCase>();
        services.AddScoped<IGetRecentOrganizationsUseCase, GetRecentOrganizationsUseCase>();
        services.AddScoped<IGetRecentActivitiesUseCase, GetRecentActivitiesUseCase>();

        return services;
    }
}

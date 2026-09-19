using AriaHR.Modules.Identity.Infrastructure;
using AriaHR.Modules.Organization.API.Controllers;
using AriaHR.Modules.Organization.Application.UseCases.CreateOrganization;
using AriaHR.Modules.Organization.Application.UseCases.GetDashboardSummary;
using AriaHR.Modules.Organization.Application.UseCases.GetRecentActivities;
using AriaHR.Modules.Organization.Application.UseCases.GetRecentOrganizations;
using AriaHR.Modules.Organization.Application.UseCases.GetTotalOrganizationsCount;
using AriaHR.Modules.Organization.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AriaHR.Modules.Organization.Tests;

public class OrganizationDependencyInjectionTests
{
    [Fact]
    public void AddOrganizationModule_ShouldResolveAllControllersAndUseCases()
    {
        // Arrange
        var services = new ServiceCollection();
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"ConnectionStrings:DefaultConnection", "Server=(localdb)\\mssqllocaldb;Database=AriaHR_Test;Trusted_Connection=True;MultipleActiveResultSets=true"}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        services.AddIdentityModule(configuration);
        services.AddOrganizationModule(configuration);

        // Register controllers explicitly to verify constructor dependency resolution
        services.AddTransient<OrganizationsController>();
        services.AddTransient<DashboardController>();

        var provider = services.BuildServiceProvider();

        // Assert - UseCases
        Assert.NotNull(provider.GetService<ICreateOrganizationUseCase>());
        Assert.NotNull(provider.GetService<IGetOrganizationsDashboardSummaryUseCase>());
        Assert.NotNull(provider.GetService<IGetTotalOrganizationsCountUseCase>());
        Assert.NotNull(provider.GetService<IGetRecentOrganizationsUseCase>());
        Assert.NotNull(provider.GetService<IGetRecentActivitiesUseCase>());

        // Assert - Controllers
        Assert.NotNull(provider.GetService<OrganizationsController>());
        Assert.NotNull(provider.GetService<DashboardController>());
    }
}

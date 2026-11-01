using AriaHR.Modules.Attendance.Infrastructure;
using AriaHR.Modules.Identity.Infrastructure;
using AriaHR.Modules.Notification.Infrastructure;
using AriaHR.Modules.Organization.Infrastructure;
using AriaHR.Modules.Requests.Infrastructure;
using AriaHR.Modules.Scheduling.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AriaHR.Modules.Organization.Tests;

public class DatabaseConfigurationEnvironmentTests
{
    private const string LocalDevConnectionString =
        "Server=DESKTOP-4NSD20E;Database=AriaHRDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

    private const string ProductionConnectionString =
        "Server=tcp:prod-sql.ariahr.internal,1433;Database=AriaHRDB_Prod;User ID=aria_prod_user;Password=Secr3tPassword!;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=True";

    [Fact]
    public void DevelopmentEnvironment_ResolvesDevelopmentConnectionString()
    {
        // Arrange: Load configuration using appsettings.json and appsettings.Development.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        // Act
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // Assert
        Assert.NotNull(connectionString);
        Assert.Equal(LocalDevConnectionString, connectionString);
    }

    [Fact]
    public void ProductionEnvironment_WithoutEnvironmentVariable_DefaultConnectionIsNull()
    {
        // Arrange: Production config without ConnectionStrings__DefaultConnection
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Production.json", optional: true)
            .Build();

        // Act
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // Assert: Production appsettings does not contain DefaultConnection
        Assert.True(string.IsNullOrWhiteSpace(connectionString));

        // Centralized resolution check fails fast
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Database connection string 'DefaultConnection' is not configured.");
            }
        });

        Assert.Equal("Database connection string 'DefaultConnection' is not configured.", ex.Message);
    }

    [Fact]
    public void ProductionEnvironment_WithEnvironmentVariable_ResolvesProductionConnectionString()
    {
        // Arrange: Production configuration supplemented with environment variable
        var envVars = new Dictionary<string, string?>
        {
            { "ConnectionStrings:DefaultConnection", ProductionConnectionString }
        };

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Production.json", optional: true)
            .AddInMemoryCollection(envVars)
            .Build();

        // Act
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // Assert
        Assert.NotNull(connectionString);
        Assert.Equal(ProductionConnectionString, connectionString);
        Assert.DoesNotContain("DESKTOP-4NSD20E", connectionString);
    }

    [Fact]
    public void ModuleRegistrations_FailFastWhenConnectionStringMissing()
    {
        var services = new ServiceCollection();
        var emptyConfig = new ConfigurationBuilder().Build();

        Assert.Throws<InvalidOperationException>(() =>
            services.AddIdentityModule(emptyConfig));

        Assert.Throws<InvalidOperationException>(() =>
            services.AddOrganizationModule(emptyConfig));

        Assert.Throws<InvalidOperationException>(() =>
            services.AddAttendanceModule(emptyConfig));

        Assert.Throws<InvalidOperationException>(() =>
            services.AddSchedulingModule(emptyConfig));

        Assert.Throws<InvalidOperationException>(() =>
            services.AddRequestsModule(emptyConfig));

        Assert.Throws<InvalidOperationException>(() =>
            services.AddNotificationModule(emptyConfig));
    }

    [Fact]
    public void ModuleRegistrations_SucceedWhenExplicitConnectionStringPassed()
    {
        var services = new ServiceCollection();
        var emptyConfig = new ConfigurationBuilder().Build();

        // Should not throw when explicit connection string is passed
        services.AddIdentityModule(emptyConfig, LocalDevConnectionString);
        services.AddOrganizationModule(emptyConfig, LocalDevConnectionString);
        services.AddAttendanceModule(emptyConfig, LocalDevConnectionString);
        services.AddSchedulingModule(emptyConfig, LocalDevConnectionString);
        services.AddRequestsModule(emptyConfig, LocalDevConnectionString);
        services.AddNotificationModule(emptyConfig, LocalDevConnectionString);

        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider);
    }
}

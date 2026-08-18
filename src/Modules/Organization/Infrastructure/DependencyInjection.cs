using AriaHR.Modules.Organization.Infrastructure.Persistence;
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
        var connectionString = configuration.GetConnectionString("OrganizationDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'OrganizationDatabase' was not configured.");
        }

        services.AddDbContext<OrganizationDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        return services;
    }
}

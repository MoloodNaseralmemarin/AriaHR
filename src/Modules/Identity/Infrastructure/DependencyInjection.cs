using AriaHR.Modules.Identity.Application.Repositories;
using AriaHR.Modules.Identity.Infrastructure.Persistence;
using AriaHR.Modules.Identity.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AriaHR.Modules.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddIdentityInfrastructure(configuration);
    }

    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("IdentityDatabase");

        services.AddDbContext<IdentityDbContext>(options =>
        {
            if (!string.IsNullOrEmpty(connectionString))
            {
                options.UseSqlServer(connectionString);
            }
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();

        return services;
    }
}

using AriaHR.Modules.Identity.Infrastructure.Persistence;
using AriaHR.Modules.Identity.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AriaHR.Modules.Identity.Infrastructure;

public static class IdentitySeederExtensions
{
    public static async Task<IHost> SeedIdentityAsync(this IHost host)
    {
        ArgumentNullException.ThrowIfNull(host);

        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<IdentityDbContext>();
        var configuration = scope.ServiceProvider.GetService<IConfiguration>();

        if (dbContext != null)
        {
            await IdentitySeeder.SeedAsync(dbContext, configuration);
        }

        return host;
    }
}

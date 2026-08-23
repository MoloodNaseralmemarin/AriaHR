using AriaHR.Modules.Identity.Infrastructure.Persistence;
using AriaHR.Modules.Identity.Infrastructure.Seed;
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

        if (dbContext != null)
        {
            await IdentitySeeder.SeedAsync(dbContext);
        }

        return host;
    }
}

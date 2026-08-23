using AriaHR.Modules.Identity.Domain.Entities;
using AriaHR.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Identity.Infrastructure.Seed;

public static class IdentitySeeder
{
    private static readonly (string Name, string Description)[] SystemRoles =
    [
        ("SystemAdmin", "System Administrator"),
        ("CenterManager", "Center Manager"),
        ("Employee", "Employee")
    ];

    public static async Task SeedAsync(IdentityDbContext dbContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        var existingRoles = await dbContext.Roles
            .Select(r => r.Name)
            .ToListAsync(cancellationToken);

        var rolesToSeed = SystemRoles
            .Where(role => !existingRoles.Contains(role.Name))
            .Select(role => new Role
            {
                Name = role.Name,
                Description = role.Description,
                CreatedAtUtc = DateTime.UtcNow
            })
            .ToList();

        if (rolesToSeed.Count > 0)
        {
            await dbContext.Roles.AddRangeAsync(rolesToSeed, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

using AriaHR.Modules.Identity.Application.Common;
using AriaHR.Modules.Identity.Application.Options;
using AriaHR.Modules.Identity.Domain.Entities;
using AriaHR.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AriaHR.Modules.Identity.Infrastructure.Seed;

public static class IdentitySeeder
{
    private static readonly (string Name, string Description)[] SystemRoles =
    [
        ("SystemAdmin", "مدیریت کل سیستم و همه مراکز"),
        ("CenterManager", "مدیر یک مرکز؛ مثلاً دکتر، نماینده یا مسئول مرکز"),
        ("Employee", "Employee")
    ];

    public static async Task SeedAsync(
        IdentityDbContext dbContext,
        IConfiguration? configuration = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        await SeedRolesAsync(dbContext, cancellationToken);

        if (configuration != null)
        {
            await SeedInitialAdminsAsync(dbContext, configuration, cancellationToken);
        }
    }

    private static async Task SeedRolesAsync(IdentityDbContext dbContext, CancellationToken cancellationToken)
    {
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

    private static async Task SeedInitialAdminsAsync(
        IdentityDbContext dbContext,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var initialAdmins = configuration
            .GetSection("Identity:InitialAdmins")
            .Get<List<InitialAdminUserOptions>>() ?? [];

        if (initialAdmins.Count == 0)
        {
            return;
        }

        var systemAdminRole = await dbContext.Roles
            .FirstOrDefaultAsync(r => r.Name == "SystemAdmin", cancellationToken);

        if (systemAdminRole == null)
        {
            return;
        }

        var now = DateTime.UtcNow;

        foreach (var adminConfig in initialAdmins)
        {
            if (string.IsNullOrWhiteSpace(adminConfig.PhoneNumber))
            {
                continue;
            }

            string normalizedMobile = MobileNumberNormalizer.Normalize(adminConfig.PhoneNumber);

            var existingUser = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == normalizedMobile || u.PhoneNumber == normalizedMobile, cancellationToken);

            if (existingUser == null)
            {
                existingUser = new User
                {
                    Id = Guid.NewGuid(),
                    Username = normalizedMobile,
                    PhoneNumber = normalizedMobile,
                    Email = string.Empty,
                    PasswordHash = string.Empty,
                    IsActive = true,
                    CreatedAtUtc = now
                };

                await dbContext.Users.AddAsync(existingUser, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            var existingUserRole = await dbContext.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == existingUser.Id && ur.RoleId == systemAdminRole.Id, cancellationToken);

            if (existingUserRole == null)
            {
                var userRole = new UserRole
                {
                    UserId = existingUser.Id,
                    RoleId = systemAdminRole.Id,
                    CreatedAtUtc = now
                };

                await dbContext.UserRoles.AddAsync(userRole, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}

using AriaHR.Modules.Identity.Domain.Entities;
using AriaHR.Modules.Identity.Infrastructure.Persistence;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.API.DataRepair;

public static class IdentityOrganizationDataRepair
{
    public static async Task RepairUnlinkedCenterManagersAsync(
        IdentityDbContext identityDb,
        OrganizationDbContext orgDb,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identityDb);
        ArgumentNullException.ThrowIfNull(orgDb);

        var unlinkedUsers = await identityDb.Users
            .Where(u => u.OrganizationId == null && !u.IsDeleted && u.PhoneNumber != null && u.PhoneNumber != "")
            .ToListAsync(cancellationToken);

        if (unlinkedUsers.Count == 0)
        {
            return;
        }

        var organizations = await orgDb.Organizations
            .Where(o => !o.IsDeleted && o.ManagerMobile != null && o.ManagerMobile != "")
            .ToListAsync(cancellationToken);

        if (organizations.Count == 0)
        {
            return;
        }

        var centerManagerRole = await identityDb.Roles
            .FirstOrDefaultAsync(r => r.Name == "CenterManager", cancellationToken);

        bool changesMade = false;

        foreach (var user in unlinkedUsers)
        {
            var matchingOrgs = organizations
                .Where(o => string.Equals(o.ManagerMobile, user.PhoneNumber, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matchingOrgs.Count == 1)
            {
                var targetOrg = matchingOrgs.Single();
                user.OrganizationId = targetOrg.Id;
                changesMade = true;

                if (centerManagerRole != null)
                {
                    bool hasRole = await identityDb.UserRoles
                        .AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == centerManagerRole.Id, cancellationToken);

                    if (!hasRole)
                    {
                        var userRole = new UserRole
                        {
                            Id = Guid.NewGuid(),
                            UserId = user.Id,
                            RoleId = centerManagerRole.Id,
                            CreatedAtUtc = DateTime.UtcNow
                        };
                        await identityDb.UserRoles.AddAsync(userRole, cancellationToken);
                    }
                }

                logger?.LogInformation(
                    "Data repair: Linked unlinked user {UserId} ({Phone}) to Organization {OrganizationId}",
                    user.Id, user.PhoneNumber, targetOrg.Id);
            }
            else if (matchingOrgs.Count > 1)
            {
                logger?.LogWarning(
                    "Data repair: Skipped unlinked user {UserId} ({Phone}) due to multiple matching organizations ({Count})",
                    user.Id, user.PhoneNumber, matchingOrgs.Count);
            }
        }

        if (changesMade)
        {
            await identityDb.SaveChangesAsync(cancellationToken);
        }
    }
}

using AriaHR.API.DataRepair;
using AriaHR.Modules.Identity.Domain.Entities;
using AriaHR.Modules.Identity.Infrastructure.Persistence;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
using OrganizationEntity = AriaHR.Modules.Organization.Domain.Entities.Organization;
using OrganizationType = AriaHR.Modules.Organization.Domain.Entities.OrganizationType;

namespace AriaHR.Modules.Organization.Tests;

public class DataRepairTests
{
    private (IdentityDbContext identityDb, OrganizationDbContext orgDb) GetInMemoryDbContexts()
    {
        string dbName = Guid.NewGuid().ToString();

        var identityOptions = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var orgOptions = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var identityDb = new IdentityDbContext(identityOptions);
        var orgDb = new OrganizationDbContext(orgOptions);

        identityDb.Roles.Add(new Role { Id = Guid.NewGuid(), Name = "CenterManager", Description = "Center Manager" });
        identityDb.SaveChanges();

        return (identityDb, orgDb);
    }

    [Fact]
    public async Task RepairUnlinkedCenterManagersAsync_RepairsSpecificUser_AndIsIdempotent()
    {
        // Arrange (Test 12: Data Repair Idempotency & Affected User Repair)
        var (identityDb, orgDb) = GetInMemoryDbContexts();

        var targetUserId = Guid.Parse("36461dba-c189-43fa-94fd-8922e8d1ab6f");
        var targetPhone = "09183194869";
        var expectedOrgId = Guid.NewGuid();

        // Target user with null OrganizationId
        var user = new User
        {
            Id = targetUserId,
            FirstName = "فاطمه",
            LastName = "سالمی",
            PhoneNumber = targetPhone,
            OrganizationId = null,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        identityDb.Users.Add(user);
        identityDb.SaveChanges();

        // Organization with matching ManagerMobile
        var org = new OrganizationEntity
        {
            Id = expectedOrgId,
            Name = "Aria Medical Center",
            Code = "AMC-01",
            Type = OrganizationType.Clinic,
            ManagerFirstName = "فاطمه",
            ManagerLastName = "سالمی",
            ManagerMobile = targetPhone,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        orgDb.Organizations.Add(org);
        orgDb.SaveChanges();

        // Act - Run 1
        await IdentityOrganizationDataRepair.RepairUnlinkedCenterManagersAsync(identityDb, orgDb);

        // Assert - Run 1
        var repairedUser = await identityDb.Users.FirstAsync(u => u.Id == targetUserId);
        Assert.Equal(expectedOrgId, repairedUser.OrganizationId);

        var centerManagerRole = await identityDb.Roles.FirstAsync(r => r.Name == "CenterManager");
        var userRole = await identityDb.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == targetUserId && ur.RoleId == centerManagerRole.Id);
        Assert.NotNull(userRole);

        int userRoleCountRun1 = await identityDb.UserRoles.CountAsync();

        // Act - Run 2 (Idempotency test)
        await IdentityOrganizationDataRepair.RepairUnlinkedCenterManagersAsync(identityDb, orgDb);

        // Assert - Run 2
        var userRun2 = await identityDb.Users.FirstAsync(u => u.Id == targetUserId);
        Assert.Equal(expectedOrgId, userRun2.OrganizationId);
        int userRoleCountRun2 = await identityDb.UserRoles.CountAsync();
        Assert.Equal(userRoleCountRun1, userRoleCountRun2); // No duplicate user roles
    }

    [Fact]
    public async Task RepairUnlinkedCenterManagersAsync_AmbiguousMatches_AreSkipped()
    {
        // Arrange (Test 13: Ambiguous Repair Data)
        var (identityDb, orgDb) = GetInMemoryDbContexts();

        var phone = "09123334455";
        var unlinkedUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Ambiguous",
            LastName = "Manager",
            PhoneNumber = phone,
            OrganizationId = null,
            IsActive = true
        };
        identityDb.Users.Add(unlinkedUser);
        identityDb.SaveChanges();

        // Two organizations with the exact same ManagerMobile
        orgDb.Organizations.AddRange(
            new OrganizationEntity { Id = Guid.NewGuid(), Name = "Org 1", Code = "O1", Type = OrganizationType.Clinic, ManagerMobile = phone },
            new OrganizationEntity { Id = Guid.NewGuid(), Name = "Org 2", Code = "O2", Type = OrganizationType.Laboratory, ManagerMobile = phone }
        );
        orgDb.SaveChanges();

        // Act
        await IdentityOrganizationDataRepair.RepairUnlinkedCenterManagersAsync(identityDb, orgDb);

        // Assert - User must be skipped and OrganizationId must remain null
        var userAfterRepair = await identityDb.Users.FirstAsync(u => u.Id == unlinkedUser.Id);
        Assert.Null(userAfterRepair.OrganizationId);
    }
}

using AriaHR.Modules.Identity.Domain.Entities;
using AriaHR.Modules.Identity.Infrastructure.Persistence;
using AriaHR.Modules.Identity.Infrastructure.Repositories;
using AriaHR.Modules.Organization.Application.UseCases.GetDashboardSummary;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AriaHR.Modules.Identity.Tests;

public class CenterManagerCountTests
{
    private readonly DbContextOptions<IdentityDbContext> _dbContextOptions;

    public CenterManagerCountTests()
    {
        _dbContextOptions = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    private IdentityDbContext CreateDbContext() => new(_dbContextOptions);

    [Fact]
    public async Task GetCountByRoleNameAsync_NoCenterManagers_ReturnsZero()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);

        // Act
        var count = await userRepo.GetCountByRoleNameAsync("CenterManager");

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GetCountByRoleNameAsync_OneActiveCenterManager_ReturnsOne()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);

        var role = new Role { Id = Guid.NewGuid(), Name = "CenterManager", Description = "Center Manager" };
        var user = new User { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe", PhoneNumber = "09120000001", Email = "john@example.com", IsActive = true, IsDeleted = false };
        var userRole = new UserRole { UserId = user.Id, RoleId = role.Id };

        await dbContext.Roles.AddAsync(role);
        await dbContext.Users.AddAsync(user);
        await dbContext.UserRoles.AddAsync(userRole);
        await dbContext.SaveChangesAsync();

        // Act
        var count = await userRepo.GetCountByRoleNameAsync("CenterManager");

        // Assert
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetCountByRoleNameAsync_MultipleActiveCenterManagers_ReturnsCorrectCount()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);

        var role = new Role { Id = Guid.NewGuid(), Name = "CenterManager", Description = "Center Manager" };
        await dbContext.Roles.AddAsync(role);

        for (int i = 1; i <= 3; i++)
        {
            var user = new User { Id = Guid.NewGuid(), FirstName = $"Manager{i}", LastName = "Test", PhoneNumber = $"0912000000{i}", Email = $"manager{i}@example.com", IsActive = true, IsDeleted = false };
            await dbContext.Users.AddAsync(user);
            await dbContext.UserRoles.AddAsync(new UserRole { UserId = user.Id, RoleId = role.Id });
        }
        await dbContext.SaveChangesAsync();

        // Act
        var count = await userRepo.GetCountByRoleNameAsync("CenterManager");

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task GetCountByRoleNameAsync_UsersWithOtherRoles_NotCounted()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);

        var cmRole = new Role { Id = Guid.NewGuid(), Name = "CenterManager", Description = "Center Manager" };
        var adminRole = new Role { Id = Guid.NewGuid(), Name = "SystemAdmin", Description = "Admin" };
        var empRole = new Role { Id = Guid.NewGuid(), Name = "Employee", Description = "Employee" };

        await dbContext.Roles.AddRangeAsync(cmRole, adminRole, empRole);

        var cmUser = new User { Id = Guid.NewGuid(), FirstName = "CM", LastName = "User", PhoneNumber = "09120000001", Email = "cm@example.com", IsActive = true, IsDeleted = false };
        var adminUser = new User { Id = Guid.NewGuid(), FirstName = "Admin", LastName = "User", PhoneNumber = "09120000002", Email = "admin@example.com", IsActive = true, IsDeleted = false };
        var empUser = new User { Id = Guid.NewGuid(), FirstName = "Emp", LastName = "User", PhoneNumber = "09120000003", Email = "emp@example.com", IsActive = true, IsDeleted = false };

        await dbContext.Users.AddRangeAsync(cmUser, adminUser, empUser);

        await dbContext.UserRoles.AddRangeAsync(
            new UserRole { UserId = cmUser.Id, RoleId = cmRole.Id },
            new UserRole { UserId = adminUser.Id, RoleId = adminRole.Id },
            new UserRole { UserId = empUser.Id, RoleId = empRole.Id }
        );
        await dbContext.SaveChangesAsync();

        // Act
        var count = await userRepo.GetCountByRoleNameAsync("CenterManager");

        // Assert
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetCountByRoleNameAsync_DeletedOrInactiveCenterManager_NotCounted()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);

        var role = new Role { Id = Guid.NewGuid(), Name = "CenterManager", Description = "Center Manager" };
        await dbContext.Roles.AddAsync(role);

        var activeUser = new User { Id = Guid.NewGuid(), FirstName = "Active", LastName = "User", PhoneNumber = "09120000001", Email = "active@example.com", IsActive = true, IsDeleted = false };
        var deletedUser = new User { Id = Guid.NewGuid(), FirstName = "Deleted", LastName = "User", PhoneNumber = "09120000002", Email = "deleted@example.com", IsActive = true, IsDeleted = true };
        var inactiveUser = new User { Id = Guid.NewGuid(), FirstName = "Inactive", LastName = "User", PhoneNumber = "09120000003", Email = "inactive@example.com", IsActive = false, IsDeleted = false };

        await dbContext.Users.AddRangeAsync(activeUser, deletedUser, inactiveUser);

        await dbContext.UserRoles.AddRangeAsync(
            new UserRole { UserId = activeUser.Id, RoleId = role.Id },
            new UserRole { UserId = deletedUser.Id, RoleId = role.Id },
            new UserRole { UserId = inactiveUser.Id, RoleId = role.Id }
        );
        await dbContext.SaveChangesAsync();

        // Act
        var count = await userRepo.GetCountByRoleNameAsync("CenterManager");

        // Assert
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetCountByRoleNameAsync_DuplicateUserRoleRelationships_CountedOnlyOnce()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);

        var role1 = new Role { Id = Guid.NewGuid(), Name = "CenterManager", Description = "Center Manager 1" };
        var role2 = new Role { Id = Guid.NewGuid(), Name = "CenterManager", Description = "Center Manager 2" };

        await dbContext.Roles.AddRangeAsync(role1, role2);

        var user = new User { Id = Guid.NewGuid(), FirstName = "MultiRole", LastName = "User", PhoneNumber = "09120000001", Email = "multirole@example.com", IsActive = true, IsDeleted = false };
        await dbContext.Users.AddAsync(user);

        await dbContext.UserRoles.AddRangeAsync(
            new UserRole { UserId = user.Id, RoleId = role1.Id },
            new UserRole { UserId = user.Id, RoleId = role2.Id }
        );
        await dbContext.SaveChangesAsync();

        // Act
        var count = await userRepo.GetCountByRoleNameAsync("CenterManager");

        // Assert
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetOrganizationsDashboardSummaryUseCase_ExecutesAndReturnsCenterManagerCount()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);

        var role = new Role { Id = Guid.NewGuid(), Name = "CenterManager", Description = "Center Manager" };
        var user = new User { Id = Guid.NewGuid(), FirstName = "CM", LastName = "User", PhoneNumber = "09120000001", Email = "cm2@example.com", IsActive = true, IsDeleted = false };

        await dbContext.Roles.AddAsync(role);
        await dbContext.Users.AddAsync(user);
        await dbContext.UserRoles.AddAsync(new UserRole { UserId = user.Id, RoleId = role.Id });
        await dbContext.SaveChangesAsync();

        var useCase = new GetOrganizationsDashboardSummaryUseCase(userRepo);

        // Act
        var result = await useCase.ExecuteAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.CenterManagerCount);
    }
}

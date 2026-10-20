using AriaHR.Modules.Identity.API.Controllers;
using AriaHR.Modules.Identity.Application.DTOs;
using AriaHR.Modules.Identity.Application.UseCases;
using AriaHR.Modules.Identity.Domain.Entities;
using AriaHR.Modules.Identity.Infrastructure.Persistence;
using AriaHR.Modules.Identity.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AriaHR.Modules.Identity.Tests;

public class GetCenterManagersTests
{
    private readonly DbContextOptions<IdentityDbContext> _dbContextOptions;

    public GetCenterManagersTests()
    {
        _dbContextOptions = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    private IdentityDbContext CreateDbContext() => new(_dbContextOptions);

    [Fact]
    public async Task GetCenterManagersAsync_ReturnsOnlyNonDeletedCenterManagersWithFieldsMapped()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);

        var cmRole = new Role { Id = Guid.NewGuid(), Name = "CenterManager", Description = "Center Manager" };
        var empRole = new Role { Id = Guid.NewGuid(), Name = "Employee", Description = "Employee" };
        await dbContext.Roles.AddRangeAsync(cmRole, empRole);

        var orgId = Guid.NewGuid();

        var activeCm = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Ali",
            LastName = "Rezai",
            PhoneNumber = "09121111111",
            Email = "ali@example.com",
            Gender = "Male",
            OrganizationId = orgId,
            IsActive = true,
            IsDeleted = false,
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2)
        };

        var inactiveCm = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Sara",
            LastName = "Ahmadi",
            PhoneNumber = "09122222222",
            Email = "sara@example.com",
            Gender = "Female",
            OrganizationId = orgId,
            IsActive = false,
            IsDeleted = false,
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1)
        };

        var deletedCm = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Deleted",
            LastName = "Manager",
            PhoneNumber = "09123333333",
            Email = "deleted@example.com",
            Gender = "Male",
            OrganizationId = orgId,
            IsActive = true,
            IsDeleted = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var employeeUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Emp",
            LastName = "User",
            PhoneNumber = "09124444444",
            Email = "emp@example.com",
            Gender = "Male",
            OrganizationId = orgId,
            IsActive = true,
            IsDeleted = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        await dbContext.Users.AddRangeAsync(activeCm, inactiveCm, deletedCm, employeeUser);

        await dbContext.UserRoles.AddRangeAsync(
            new UserRole { UserId = activeCm.Id, RoleId = cmRole.Id },
            new UserRole { UserId = inactiveCm.Id, RoleId = cmRole.Id },
            new UserRole { UserId = deletedCm.Id, RoleId = cmRole.Id },
            new UserRole { UserId = employeeUser.Id, RoleId = empRole.Id }
        );

        await dbContext.SaveChangesAsync();

        // Act
        var result = await userRepo.GetCenterManagersAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, u => u.Id == activeCm.Id);
        Assert.Contains(result, u => u.Id == inactiveCm.Id);
        Assert.DoesNotContain(result, u => u.Id == deletedCm.Id);
        Assert.DoesNotContain(result, u => u.Id == employeeUser.Id);

        var aliDto = result.First(u => u.Id == activeCm.Id);
        Assert.Equal("Ali", aliDto.FirstName);
        Assert.Equal("Rezai", aliDto.LastName);
        Assert.Equal("Ali Rezai", aliDto.FullName);
        Assert.Equal("09121111111", aliDto.PhoneNumber);
        Assert.Equal("ali@example.com", aliDto.Email);
        Assert.Equal("Male", aliDto.Gender);
        Assert.Equal(orgId, aliDto.OrganizationId);
        Assert.True(aliDto.IsActive);
    }

    [Fact]
    public async Task GetCenterManagersAsync_FiltersByOrganizationIdWhenProvided()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);

        var cmRole = new Role { Id = Guid.NewGuid(), Name = "CenterManager", Description = "Center Manager" };
        await dbContext.Roles.AddAsync(cmRole);

        var org1 = Guid.NewGuid();
        var org2 = Guid.NewGuid();

        var cmOrg1 = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Manager1",
            LastName = "Org1",
            PhoneNumber = "09120000001",
            OrganizationId = org1,
            IsActive = true,
            IsDeleted = false
        };

        var cmOrg2 = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Manager2",
            LastName = "Org2",
            PhoneNumber = "09120000002",
            OrganizationId = org2,
            IsActive = true,
            IsDeleted = false
        };

        await dbContext.Users.AddRangeAsync(cmOrg1, cmOrg2);

        await dbContext.UserRoles.AddRangeAsync(
            new UserRole { UserId = cmOrg1.Id, RoleId = cmRole.Id },
            new UserRole { UserId = cmOrg2.Id, RoleId = cmRole.Id }
        );

        await dbContext.SaveChangesAsync();

        // Act
        var resultOrg1 = await userRepo.GetCenterManagersAsync(org1);
        var resultAll = await userRepo.GetCenterManagersAsync();

        // Assert
        Assert.Single(resultOrg1);
        Assert.Equal(cmOrg1.Id, resultOrg1[0].Id);

        Assert.Equal(2, resultAll.Count);
    }

    [Fact]
    public async Task GetCenterManagersAsync_UserWithMultipleCenterManagerRoles_ReturnedOnlyOnce()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);

        var role1 = new Role { Id = Guid.NewGuid(), Name = "CenterManager", Description = "Role 1" };
        var role2 = new Role { Id = Guid.NewGuid(), Name = "CenterManager", Description = "Role 2" };

        await dbContext.Roles.AddRangeAsync(role1, role2);

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "MultiRole",
            LastName = "User",
            PhoneNumber = "09120000001",
            IsActive = true,
            IsDeleted = false
        };

        await dbContext.Users.AddAsync(user);

        await dbContext.UserRoles.AddRangeAsync(
            new UserRole { UserId = user.Id, RoleId = role1.Id },
            new UserRole { UserId = user.Id, RoleId = role2.Id }
        );

        await dbContext.SaveChangesAsync();

        // Act
        var result = await userRepo.GetCenterManagersAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(user.Id, result[0].Id);
    }

    [Fact]
    public async Task GetCenterManagersUseCase_DelegatesToRepositoryAndReturnsList()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);

        var role = new Role { Id = Guid.NewGuid(), Name = "CenterManager", Description = "Center Manager" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "UseCase",
            LastName = "Test",
            PhoneNumber = "09120000001",
            IsActive = true,
            IsDeleted = false
        };

        await dbContext.Roles.AddAsync(role);
        await dbContext.Users.AddAsync(user);
        await dbContext.UserRoles.AddAsync(new UserRole { UserId = user.Id, RoleId = role.Id });
        await dbContext.SaveChangesAsync();

        var useCase = new GetCenterManagersUseCase(userRepo);

        // Act
        var result = await useCase.ExecuteAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(user.Id, result[0].Id);
    }

    [Fact]
    public async Task UsersController_GetCenterManagers_ReturnsOkWithList()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);

        var role = new Role { Id = Guid.NewGuid(), Name = "CenterManager", Description = "Center Manager" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Controller",
            LastName = "Test",
            PhoneNumber = "09120000001",
            IsActive = true,
            IsDeleted = false
        };

        await dbContext.Roles.AddAsync(role);
        await dbContext.Users.AddAsync(user);
        await dbContext.UserRoles.AddAsync(new UserRole { UserId = user.Id, RoleId = role.Id });
        await dbContext.SaveChangesAsync();

        var useCase = new GetCenterManagersUseCase(userRepo);
        var controller = new UsersController(useCase);

        // Act
        var actionResult = await controller.GetCenterManagers(null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var dtos = Assert.IsAssignableFrom<IEnumerable<CenterManagerDto>>(okResult.Value);
        Assert.Single(dtos);
    }
}

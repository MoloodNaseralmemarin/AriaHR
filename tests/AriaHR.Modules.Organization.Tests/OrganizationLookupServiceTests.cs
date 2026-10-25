using AriaHR.Modules.Organization.Domain.Entities;
using AriaHR.Modules.Organization.Domain.Enums;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using AriaHR.Modules.Organization.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AriaHR.Modules.Organization.Tests;

public class OrganizationLookupServiceTests
{
    private readonly DbContextOptions<OrganizationDbContext> _dbContextOptions;

    public OrganizationLookupServiceTests()
    {
        _dbContextOptions = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    private OrganizationDbContext CreateDbContext() => new(_dbContextOptions);

    [Fact]
    public async Task GetOrganizationNameForUserAsync_WithValidOrganizationId_ReturnsOrganizationName()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var org = new Domain.Entities.Organization
        {
            Id = Guid.NewGuid(),
            Name = "مرکز پزشکی شفا",
            Code = "C001",
            OrganizationType = OrganizationType.Clinic,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        await dbContext.Organizations.AddAsync(org);
        await dbContext.SaveChangesAsync();

        var service = new OrganizationLookupService(dbContext);

        // Act
        var result = await service.GetOrganizationNameForUserAsync(org.Id, "09120001122", Guid.NewGuid());

        // Assert
        Assert.Equal("مرکز پزشکی شفا", result);
    }

    [Fact]
    public async Task GetOrganizationNameForUserAsync_WithNullOrganizationId_FallbackToManagerMobile_ReturnsOrganizationName()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var org = new Domain.Entities.Organization
        {
            Id = Guid.NewGuid(),
            Name = "کلینیک تخصص نور",
            Code = "C002",
            ManagerMobile = "09123456789",
            OrganizationType = OrganizationType.Clinic,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        await dbContext.Organizations.AddAsync(org);
        await dbContext.SaveChangesAsync();

        var service = new OrganizationLookupService(dbContext);

        // Act
        var result = await service.GetOrganizationNameForUserAsync(null, "09123456789", Guid.NewGuid());

        // Assert
        Assert.Equal("کلینیک تخصص نور", result);
    }

    [Fact]
    public async Task GetOrganizationNameForUserAsync_WithNullOrganizationId_FallbackToEmployeeUserId_ReturnsOrganizationName()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var org = new Domain.Entities.Organization
        {
            Id = Guid.NewGuid(),
            Name = "بیمارستان مهر",
            Code = "H001",
            OrganizationType = OrganizationType.Clinic,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrganizationId = org.Id,
            PersonnelCode = "P001",
            NationalCode = "1234567890",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        await dbContext.Organizations.AddAsync(org);
        await dbContext.Employees.AddAsync(employee);
        await dbContext.SaveChangesAsync();

        var service = new OrganizationLookupService(dbContext);

        // Act
        var result = await service.GetOrganizationNameForUserAsync(null, "09990001122", userId);

        // Assert
        Assert.Equal("بیمارستان مهر", result);
    }

    [Fact]
    public async Task GetOrganizationNameForUserAsync_WithNoAssociatedOrganization_ReturnsNull()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var service = new OrganizationLookupService(dbContext);

        // Act
        var result = await service.GetOrganizationNameForUserAsync(null, "09120000000", Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }
}

using AriaHR.Modules.Organization.API.Controllers;
using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.UseCases.GetDashboardSummary;
using AriaHR.Modules.Organization.Application.UseCases.GetRecentActivities;
using AriaHR.Modules.Organization.Application.UseCases.GetRecentOrganizations;
using AriaHR.Modules.Organization.Domain.Entities;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using AriaHR.Modules.Organization.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AriaHR.Modules.Organization.Tests;

public class DashboardApiTests
{
    private OrganizationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new OrganizationDbContext(options);
    }


    [Fact]
    public async Task RecentOrganizations_ReturnsLatest3NonDeletedOrganizationsOrderedDescending()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        var baseTime = DateTime.UtcNow;

        var org1 = new Domain.Entities.Organization { Id = Guid.NewGuid(), Name = "Org 1", Code = "O1", CreatedAtUtc = baseTime.AddHours(-10), IsDeleted = false };
        var org2 = new Domain.Entities.Organization { Id = Guid.NewGuid(), Name = "Org 2", Code = "O2", CreatedAtUtc = baseTime.AddHours(-5), IsDeleted = false };
        var org3 = new Domain.Entities.Organization { Id = Guid.NewGuid(), Name = "Org 3", Code = "O3", CreatedAtUtc = baseTime.AddHours(-1), IsDeleted = false };
        var org4 = new Domain.Entities.Organization { Id = Guid.NewGuid(), Name = "Org 4", Code = "O4", CreatedAtUtc = baseTime.AddHours(-20), IsDeleted = false };
        var deletedOrg = new Domain.Entities.Organization { Id = Guid.NewGuid(), Name = "Deleted", Code = "O5", CreatedAtUtc = baseTime, IsDeleted = true };

        dbContext.Organizations.AddRange(org1, org2, org3, org4, deletedOrg);
        await dbContext.SaveChangesAsync();

        var repository = new OrganizationRepository(dbContext);
        var useCase = new GetRecentOrganizationsUseCase(repository);

        // Act
        var result = await useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("Org 3", result[0].Name);
        Assert.Equal("Org 2", result[1].Name);
        Assert.Equal("Org 1", result[2].Name);
    }

    [Fact]
    public async Task RecentActivities_ReturnsLatest3ActivitiesWithCorrectProperties()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        var baseTime = DateTime.UtcNow;

        var createdOrg = new Domain.Entities.Organization
        {
            Id = Guid.NewGuid(),
            Name = "Aria Clinic",
            Code = "C1",
            ManagerFirstName = "Dr. Ali",
            ManagerLastName = "Ahmadi",
            CreatedAtUtc = baseTime.AddHours(-2),
            IsActive = true,
            IsDeleted = false
        };

        var deactivatedOrg = new Domain.Entities.Organization
        {
            Id = Guid.NewGuid(),
            Name = "Imaging X",
            Code = "X1",
            CreatedAtUtc = baseTime.AddHours(-10),
            UpdatedAtUtc = baseTime.AddHours(-1),
            IsActive = false,
            IsDeleted = false
        };

        dbContext.Organizations.AddRange(createdOrg, deactivatedOrg);
        await dbContext.SaveChangesAsync();

        var repository = new OrganizationRepository(dbContext);
        var useCase = new GetRecentActivitiesUseCase(repository);

        // Act
        var result = await useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);

        // Latest activity should be deactivation at baseTime - 1 hour
        Assert.Equal("OrganizationDeactivated", result[0].Type);
        Assert.Equal("مرکز غیرفعال شد", result[0].Title);
        Assert.Equal("Imaging X", result[0].Description);
        Assert.Equal(deactivatedOrg.UpdatedAtUtc!.Value, result[0].CreatedAtUtc);

        // Next activity should be manager or org creation at baseTime - 2 hours
        Assert.Contains(result, a => a.Type == "OrganizationCreated" && a.Description == "Aria Clinic");
        Assert.Contains(result, a => a.Type == "CenterManagerCreated" && a.Description == "Dr. Ali Ahmadi");
    }

    [Fact]
    public async Task DashboardController_GetRecentActivities_ReturnsOkResult()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        dbContext.Organizations.Add(new Domain.Entities.Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Center",
            Code = "TC",
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        });
        await dbContext.SaveChangesAsync();

        var repository = new OrganizationRepository(dbContext);
        var useCase = new GetRecentActivitiesUseCase(repository);
        var controller = new DashboardController(useCase);

        // Act
        var actionResult = await controller.GetRecentActivities(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

        var activities = Assert.IsAssignableFrom<IEnumerable<RecentActivityDto>>(okResult.Value);
        Assert.NotEmpty(activities);
    }
}

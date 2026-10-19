using AriaHR.Modules.Identity.Application.Repositories;
using AriaHR.Modules.Organization.API.Controllers;
using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Repositories;
using AriaHR.Modules.Organization.Application.UseCases.GetRecentActivities;
using AriaHR.Modules.Organization.Application.UseCases.GetRecentCenters;
using AriaHR.Modules.Organization.Application.UseCases.GetSystemAdminDashboardOverview;
using AriaHR.Modules.Organization.Domain.Entities;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using AriaHR.Modules.Organization.Infrastructure.Repositories;
using AriaHR.Shared.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace AriaHR.Modules.Organization.Tests;

public class SystemAdminDashboardTests
{
    private OrganizationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new OrganizationDbContext(options);
    }

    [Fact]
    public async Task GetCentersOverviewAsync_CalculatesCountsCorrectly()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();

        // Org 1: Active, complete with WorkLocation
        var org1 = new Domain.Entities.Organization
        {
            Id = Guid.NewGuid(),
            Name = "Center 1",
            Code = "C1",
            IsActive = true,
            NationalIdentifier = "1234567890",
            Phone = "02112345678",
            Address = "Tehran",
            IsDeleted = false
        };
        org1.WorkLocations.Add(new WorkLocation { Id = Guid.NewGuid(), IsActive = true, IsDeleted = false });

        // Org 2: Active, missing Address (Awaiting Completion)
        var org2 = new Domain.Entities.Organization
        {
            Id = Guid.NewGuid(),
            Name = "Center 2",
            Code = "C2",
            IsActive = true,
            NationalIdentifier = "9876543210",
            Phone = "02187654321",
            Address = null, // Missing Address
            IsDeleted = false
        };

        // Org 3: Inactive, deleted (should be ignored)
        var org3 = new Domain.Entities.Organization
        {
            Id = Guid.NewGuid(),
            Name = "Deleted Center",
            Code = "C3",
            IsActive = false,
            IsDeleted = true
        };

        dbContext.Organizations.AddRange(org1, org2, org3);
        await dbContext.SaveChangesAsync();

        var repo = new OrganizationRepository(dbContext);

        // Act
        var (total, active, awaitingCompletion) = await repo.GetCentersOverviewAsync();

        // Assert
        Assert.Equal(2, total);
        Assert.Equal(2, active);
        Assert.Equal(1, awaitingCompletion);
    }

    [Fact]
    public async Task GetEmployeesSummary_CalculatesCreatedThisMonthWithJalaliCalendar()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();

        var nowUtc = DateTime.UtcNow;
        var (startOfJalaliMonthUtc, _) = JalaliCalendarHelper.GetCurrentJalaliMonthBoundariesUtc(nowUtc);

        var emp1 = new Employee
        {
            Id = Guid.NewGuid(),
            PersonnelCode = "101",
            NationalCode = "1111111111",
            CreatedAtUtc = startOfJalaliMonthUtc.AddDays(1), // In current Jalali month
            IsDeleted = false
        };

        var emp2 = new Employee
        {
            Id = Guid.NewGuid(),
            PersonnelCode = "102",
            NationalCode = "2222222222",
            CreatedAtUtc = startOfJalaliMonthUtc.AddDays(-10), // Before current Jalali month
            IsDeleted = false
        };

        dbContext.Employees.AddRange(emp1, emp2);
        await dbContext.SaveChangesAsync();

        var repo = new EmployeeRepository(dbContext);

        // Act
        var (totalCount, createdThisMonth) = await repo.GetEmployeesSummaryAsync(nowUtc);

        // Assert
        Assert.Equal(2, totalCount);
        Assert.Equal(1, createdThisMonth);
    }

    [Fact]
    public async Task GetRecentCentersPagedAsync_ReturnsPagedAndSortedCenters()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        var baseTime = DateTime.UtcNow;

        var org1 = new Domain.Entities.Organization
        {
            Id = Guid.NewGuid(),
            Name = "Center A",
            Code = "CA",
            ManagerFirstName = "Reza",
            ManagerLastName = "Rahimi",
            CreatedAtUtc = baseTime.AddMinutes(-30),
            IsDeleted = false
        };

        var org2 = new Domain.Entities.Organization
        {
            Id = Guid.NewGuid(),
            Name = "Center B",
            Code = "CB",
            ManagerFirstName = "Sara",
            ManagerLastName = "Ahmadi",
            CreatedAtUtc = baseTime.AddMinutes(-10),
            IsDeleted = false
        };

        dbContext.Organizations.AddRange(org1, org2);
        await dbContext.SaveChangesAsync();

        var repo = new OrganizationRepository(dbContext);

        // Act
        var result = await repo.GetRecentCentersPagedAsync(page: 1, pageSize: 5);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Center B", result.Items[0].Name);
        Assert.Equal("Sara Ahmadi", result.Items[0].ManagerName);
        Assert.Equal("Center A", result.Items[1].Name);
    }

    [Fact]
    public async Task GetSystemAdminDashboardOverviewUseCase_ExecutesAndCombinesSummaries()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();

        var orgRepoMock = new Mock<IOrganizationRepository>();
        orgRepoMock.Setup(r => r.GetCentersOverviewAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((10, 8, 2));

        var empRepoMock = new Mock<IEmployeeRepository>();
        empRepoMock.Setup(r => r.GetEmployeesSummaryAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((50, 5));

        var userRepoMock = new Mock<IUserRepository>();
        userRepoMock.Setup(u => u.GetCountByRoleNameAsync("CenterManager", It.IsAny<CancellationToken>()))
            .ReturnsAsync(8);

        var useCase = new GetSystemAdminDashboardOverviewUseCase(
            orgRepoMock.Object,
            empRepoMock.Object,
            userRepoMock.Object);

        // Act
        var overview = await useCase.ExecuteAsync();

        // Assert
        Assert.NotNull(overview);
        Assert.Equal(10, overview.Centers.TotalCount);
        Assert.Equal(8, overview.Centers.ActiveCount);
        Assert.Equal(2, overview.Centers.AwaitingCompletionCount);
        Assert.Equal(8, overview.CenterManagers.TotalCount);
        Assert.Equal(50, overview.Employees.TotalCount);
        Assert.Equal(5, overview.Employees.CreatedThisMonth);
    }

    [Fact]
    public async Task SystemAdminDashboardController_Endpoints_ReturnOkStatus()
    {
        // Arrange
        var overviewUseCaseMock = new Mock<IGetSystemAdminDashboardOverviewUseCase>();
        overviewUseCaseMock.Setup(u => u.ExecuteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SystemAdminDashboardOverviewDto());

        var recentCentersUseCaseMock = new Mock<IGetRecentCentersUseCase>();
        recentCentersUseCaseMock.Setup(u => u.ExecuteAsync(1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<RecentCenterDto>
            {
                Items = new List<RecentCenterDto> { new RecentCenterDto { Name = "Test Center" } },
                Page = 1,
                PageSize = 5,
                TotalCount = 1
            });

        var recentActivitiesUseCaseMock = new Mock<IGetSystemAdminRecentActivitiesUseCase>();
        recentActivitiesUseCaseMock.Setup(u => u.ExecuteAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<RecentActivityDto>
            {
                Items = new List<RecentActivityDto> { new RecentActivityDto { Title = "Test Activity" } },
                Page = 1,
                PageSize = 10,
                TotalCount = 1
            });

        var controller = new SystemAdminDashboardController(
            overviewUseCaseMock.Object,
            recentCentersUseCaseMock.Object,
            recentActivitiesUseCaseMock.Object);

        // Act & Assert Overview
        var overviewRes = await controller.GetOverview(CancellationToken.None);
        var okOverview = Assert.IsType<OkObjectResult>(overviewRes);
        Assert.Equal(StatusCodes.Status200OK, okOverview.StatusCode);

        // Act & Assert Recent Centers
        var centersRes = await controller.GetRecentCenters(1, 5, CancellationToken.None);
        var okCenters = Assert.IsType<OkObjectResult>(centersRes);
        Assert.Equal(StatusCodes.Status200OK, okCenters.StatusCode);

        // Act & Assert Recent Activities
        var activitiesRes = await controller.GetRecentActivities(1, 10, CancellationToken.None);
        var okActivities = Assert.IsType<OkObjectResult>(activitiesRes);
        Assert.Equal(StatusCodes.Status200OK, okActivities.StatusCode);
    }
}

using System.Reflection;
using System.Security.Claims;
using AriaHR.Modules.Identity.Infrastructure.Persistence;
using AriaHR.Modules.Identity.Infrastructure.Repositories;
using AriaHR.Modules.Organization.API.Controllers;
using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Services;
using AriaHR.Modules.Organization.Application.UseCases.CreateOrganization;
using AriaHR.Modules.Organization.Application.UseCases.GetDashboardSummary;
using AriaHR.Modules.Organization.Application.UseCases.GetRecentOrganizations;
using AriaHR.Modules.Organization.Application.UseCases.GetTotalOrganizationsCount;
using AriaHR.Modules.Organization.Domain.Entities;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using AriaHR.Modules.Organization.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AriaHR.Modules.Organization.Tests;

public class OrganizationCountTests
{
    private OrganizationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new OrganizationDbContext(options);
    }

    [Fact]
    public async Task CountAsync_WithEmptyDatabase_ReturnsZero()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        var repository = new OrganizationRepository(dbContext);

        // Act
        var count = await repository.CountAsync(CancellationToken.None);

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task CountAsync_FiltersOutDeletedAndInactiveOrganizations()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        dbContext.Organizations.AddRange(
            new Domain.Entities.Organization { Id = Guid.NewGuid(), Name = "Org 1", Code = "O1", IsActive = true, IsDeleted = false },
            new Domain.Entities.Organization { Id = Guid.NewGuid(), Name = "Org 2", Code = "O2", IsActive = true, IsDeleted = false },
            new Domain.Entities.Organization { Id = Guid.NewGuid(), Name = "Inactive Org", Code = "O3", IsActive = false, IsDeleted = false },
            new Domain.Entities.Organization { Id = Guid.NewGuid(), Name = "Deleted Org", Code = "O4", IsActive = true, IsDeleted = true },
            new Domain.Entities.Organization { Id = Guid.NewGuid(), Name = "Inactive & Deleted Org", Code = "O5", IsActive = false, IsDeleted = true }
        );
        await dbContext.SaveChangesAsync();

        var repository = new OrganizationRepository(dbContext);

        // Act
        var count = await repository.CountAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task UseCase_ExecuteAsync_ReturnsOrganizationCountResponse()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        dbContext.Organizations.AddRange(
            new Domain.Entities.Organization { Id = Guid.NewGuid(), Name = "Active Org 1", Code = "A1", IsActive = true, IsDeleted = false },
            new Domain.Entities.Organization { Id = Guid.NewGuid(), Name = "Active Org 2", Code = "A2", IsActive = true, IsDeleted = false },
            new Domain.Entities.Organization { Id = Guid.NewGuid(), Name = "Active Org 3", Code = "A3", IsActive = true, IsDeleted = false }
        );
        await dbContext.SaveChangesAsync();

        var repository = new OrganizationRepository(dbContext);
        var useCase = new GetTotalOrganizationsCountUseCase(repository);

        // Act
        var result = await useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public async Task Controller_GetCount_ReturnsOkResultWithCount()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        dbContext.Organizations.Add(
            new Domain.Entities.Organization { Id = Guid.NewGuid(), Name = "Active Org", Code = "AO", IsActive = true, IsDeleted = false }
        );
        await dbContext.SaveChangesAsync();

        var repository = new OrganizationRepository(dbContext);
        var managerService = new DummyOrganizationManagerIdentityService();
        var createUseCase = new CreateOrganizationUseCase(managerService);
        var countUseCase = new GetTotalOrganizationsCountUseCase(repository);
        var identityDb = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var userRepo = new UserRepository(identityDb);
        var summaryUseCase = new GetOrganizationsDashboardSummaryUseCase(userRepo);
        var recentUseCase = new GetRecentOrganizationsUseCase(repository);
        var controller = new OrganizationsController(createUseCase, countUseCase, summaryUseCase, recentUseCase);

        // Act
        var actionResult = await controller.GetCount(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

        var response = Assert.IsType<OrganizationCountResponse>(okResult.Value);
        Assert.Equal(1, response.TotalCount);
    }

    private class DummyOrganizationManagerIdentityService : IOrganizationManagerIdentityService
    {
        public Task<OrganizationDto> CreateOrganizationWithManagerAsync(CreateOrganizationRequest request, Guid createdByUserId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}

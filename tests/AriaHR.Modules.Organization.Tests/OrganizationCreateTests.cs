using System.Reflection;
using System.Security.Claims;
using AriaHR.Modules.Organization.API.Controllers;
using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.UseCases.CreateOrganization;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using AriaHR.Modules.Organization.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AriaHR.Modules.Organization.Tests;

public class OrganizationCreateTests
{
    private OrganizationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new OrganizationDbContext(options);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRequest_CreatesOrganizationWithoutDepartmentOrBranch()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        var repository = new OrganizationRepository(dbContext);
        var useCase = new CreateOrganizationUseCase(repository);

        var expectedCreatorId = Guid.NewGuid();
        var request = new CreateOrganizationRequest
        {
            Name = "Aria Private Practice",
            Code = "ORG-001",
            NationalIdentifier = "1234567890",
            Phone = "+123456789",
            Address = "123 Health St",
            IsActive = true
        };

        // Act
        var result = await useCase.ExecuteAsync(request, expectedCreatorId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Aria Private Practice", result.Name);
        Assert.Equal("ORG-001", result.Code);
        Assert.Equal("1234567890", result.NationalIdentifier);
        Assert.Equal("+123456789", result.Phone);
        Assert.Equal("123 Health St", result.Address);
        Assert.True(result.IsActive);
        Assert.Equal(expectedCreatorId, result.CreatedByUserId);

        // Verify database persistence
        var dbOrg = await dbContext.Organizations.FirstOrDefaultAsync(o => o.Id == result.Id);
        Assert.NotNull(dbOrg);
        Assert.Equal(expectedCreatorId, dbOrg.CreatedByUserId);

        // Verify no departments or branches were automatically created
        Assert.Empty(dbContext.Departments);
        Assert.Empty(dbContext.Branches);
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingName_ThrowsArgumentException()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        var repository = new OrganizationRepository(dbContext);
        var useCase = new CreateOrganizationUseCase(repository);

        var request = new CreateOrganizationRequest
        {
            Name = "",
            Code = "ORG-001"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, Guid.NewGuid()));
    }

    [Fact]
    public async Task Controller_Create_WithValidSystemAdminClaims_Returns201Created()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        var repository = new OrganizationRepository(dbContext);
        var useCase = new CreateOrganizationUseCase(repository);
        var controller = new OrganizationsController(useCase);

        var expectedUserId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, expectedUserId.ToString()),
            new Claim(ClaimTypes.Role, "SystemAdmin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var request = new CreateOrganizationRequest
        {
            Name = "SystemAdmin Org",
            Code = "SA-001"
        };

        // Act
        var actionResult = await controller.Create(request, CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);

        var dto = Assert.IsType<OrganizationDto>(objectResult.Value);
        Assert.Equal("SystemAdmin Org", dto.Name);
        Assert.Equal(expectedUserId, dto.CreatedByUserId);
    }

    [Fact]
    public async Task Controller_Create_WithoutUserIdClaim_Returns401Unauthorized()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        var repository = new OrganizationRepository(dbContext);
        var useCase = new CreateOrganizationUseCase(repository);
        var controller = new OrganizationsController(useCase);

        // User identity without NameIdentifier claim
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var request = new CreateOrganizationRequest
        {
            Name = "Unauthorized Org",
            Code = "UA-001"
        };

        // Act
        var actionResult = await controller.Create(request, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedResult>(actionResult);
    }

    [Fact]
    public void Controller_HasAuthorizeAttribute_RestrictedToSystemAdmin()
    {
        // Arrange & Act
        var controllerType = typeof(OrganizationsController);
        var authorizeAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        // Assert
        Assert.NotNull(authorizeAttr);
        Assert.Equal("SystemAdmin", authorizeAttr.Roles);
    }
}

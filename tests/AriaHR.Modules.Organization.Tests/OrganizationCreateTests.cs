using System.Reflection;
using System.Security.Claims;
using AriaHR.Modules.Organization.API.Controllers;
using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.UseCases.CreateOrganization;
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

public class OrganizationCreateTests
{
    private OrganizationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new OrganizationDbContext(options);
    }

    [Theory]
    [InlineData(OrganizationType.Clinic)]
    [InlineData(OrganizationType.ImagingCenter)]
    [InlineData(OrganizationType.MedicalOffice)]
    [InlineData(OrganizationType.Laboratory)]
    [InlineData(OrganizationType.Pharmacy)]
    public async Task ExecuteAsync_WithEachValidOrganizationType_CreatesAndPersistsOrganization(OrganizationType orgType)
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        var repository = new OrganizationRepository(dbContext);
        var useCase = new CreateOrganizationUseCase(repository);

        var expectedCreatorId = Guid.NewGuid();
        var request = new CreateOrganizationRequest
        {
            Name = $"Aria Health Center {orgType}",
            Code = $"ORG-{orgType}",
            Type = orgType,
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
        Assert.Equal($"Aria Health Center {orgType}", result.Name);
        Assert.Equal($"ORG-{orgType}", result.Code);
        Assert.Equal(orgType, result.Type);
        Assert.Equal("1234567890", result.NationalIdentifier);
        Assert.Equal("+123456789", result.Phone);
        Assert.Equal("123 Health St", result.Address);
        Assert.True(result.IsActive);
        Assert.Equal(expectedCreatorId, result.CreatedByUserId);

        // Verify database persistence
        var dbOrg = await dbContext.Organizations.FirstOrDefaultAsync(o => o.Id == result.Id);
        Assert.NotNull(dbOrg);
        Assert.Equal(orgType, dbOrg.Type);
        Assert.Equal(expectedCreatorId, dbOrg.CreatedByUserId);

        // Verify no departments or branches were automatically created
        Assert.Empty(dbContext.Departments);
        Assert.Empty(dbContext.Branches);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidOrganizationType_ThrowsArgumentException()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        var repository = new OrganizationRepository(dbContext);
        var useCase = new CreateOrganizationUseCase(repository);

        var request = new CreateOrganizationRequest
        {
            Name = "Invalid Type Org",
            Code = "INVALID-001",
            Type = (OrganizationType)99
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, Guid.NewGuid()));
        Assert.Contains("Invalid Organization Type", ex.Message);
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
            Code = "ORG-001",
            Type = OrganizationType.Clinic
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
        var countUseCase = new GetTotalOrganizationsCountUseCase(repository);
        var controller = new OrganizationsController(useCase, countUseCase);

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
            Code = "SA-001",
            Type = OrganizationType.ImagingCenter
        };

        // Act
        var actionResult = await controller.Create(request, CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);

        var dto = Assert.IsType<OrganizationDto>(objectResult.Value);
        Assert.Equal("SystemAdmin Org", dto.Name);
        Assert.Equal(OrganizationType.ImagingCenter, dto.Type);
        Assert.Equal(expectedUserId, dto.CreatedByUserId);
    }

    [Fact]
    public async Task Controller_Create_WithInvalidType_Returns400BadRequest()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        var repository = new OrganizationRepository(dbContext);
        var useCase = new CreateOrganizationUseCase(repository);
        var countUseCase = new GetTotalOrganizationsCountUseCase(repository);
        var controller = new OrganizationsController(useCase, countUseCase);

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
            Name = "Bad Type Org",
            Code = "BAD-001",
            Type = (OrganizationType)99
        };

        // Act
        var actionResult = await controller.Create(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);

        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problemDetails.Status);
        Assert.Equal("ورودی نامعتبر", problemDetails.Title);
        Assert.Contains("Invalid Organization Type", problemDetails.Detail);
    }

    [Fact]
    public async Task Controller_Create_WithNullRequest_Returns400BadRequest()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        var repository = new OrganizationRepository(dbContext);
        var useCase = new CreateOrganizationUseCase(repository);
        var countUseCase = new GetTotalOrganizationsCountUseCase(repository);
        var controller = new OrganizationsController(useCase, countUseCase);

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

        // Act
        var actionResult = await controller.Create(null!, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);

        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problemDetails.Status);
    }

    [Fact]
    public async Task Controller_Create_WithoutUserIdClaim_Returns401Unauthorized()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        var repository = new OrganizationRepository(dbContext);
        var useCase = new CreateOrganizationUseCase(repository);
        var countUseCase = new GetTotalOrganizationsCountUseCase(repository);
        var controller = new OrganizationsController(useCase, countUseCase);

        // User identity without NameIdentifier claim
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var request = new CreateOrganizationRequest
        {
            Name = "Unauthorized Org",
            Code = "UA-001",
            Type = OrganizationType.Clinic
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

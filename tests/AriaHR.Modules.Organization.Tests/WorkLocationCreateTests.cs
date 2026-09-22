using System.Reflection;
using System.Security.Claims;
using AriaHR.Modules.Organization.API.Controllers;
using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.UseCases.CreateWorkLocation;
using AriaHR.Modules.Organization.Domain.Entities;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using AriaHR.Modules.Organization.Infrastructure.Repositories;
using AriaHR.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AriaHR.Modules.Organization.Tests;

public class WorkLocationCreateTests
{
    private OrganizationDbContext GetInMemoryDbContext()
    {
        string dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new OrganizationDbContext(options);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRequest_CreatesWorkLocationSuccessfully()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var orgId = Guid.NewGuid();
        db.Organizations.Add(new Domain.Entities.Organization
        {
            Id = orgId,
            Name = "Tehran Main Clinic",
            Code = "TMC-01",
            Type = OrganizationType.Clinic,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var repo = new WorkLocationRepository(db);
        var useCase = new CreateWorkLocationUseCase(repo);

        var request = new CreateWorkLocationRequest
        {
            Name = "Main Entrance Site",
            Address = "123 Freedom Ave, Tehran",
            Latitude = 35.6892,
            Longitude = 51.3890,
            RadiusInMeters = 100,
            IsActive = true
        };

        var creatorId = Guid.NewGuid();

        // Act
        var result = await useCase.ExecuteAsync(request, orgId, creatorId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(orgId, result.OrganizationId);
        Assert.Equal("Main Entrance Site", result.Name);
        Assert.Equal("123 Freedom Ave, Tehran", result.Address);
        Assert.Equal(35.6892, result.Latitude);
        Assert.Equal(51.3890, result.Longitude);
        Assert.Equal(100, result.RadiusInMeters);
        Assert.True(result.IsActive);
        Assert.Equal(creatorId, result.CreatedByUserId);

        var dbLocation = await db.WorkLocations.FirstOrDefaultAsync(w => w.Id == result.Id);
        Assert.NotNull(dbLocation);
        Assert.Equal(orgId, dbLocation.OrganizationId);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var repo = new WorkLocationRepository(db);
        var useCase = new CreateWorkLocationUseCase(repo);

        var request = new CreateWorkLocationRequest
        {
            Name = "   ",
            Latitude = 35.0,
            Longitude = 51.0,
            RadiusInMeters = 50
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, Guid.NewGuid(), Guid.NewGuid()));
        Assert.Contains("نام محل کار الزامی است", ex.Message);
    }

    [Theory]
    [InlineData(90.1)]
    [InlineData(-90.1)]
    [InlineData(100.0)]
    [InlineData(-180.01)]
    public async Task ExecuteAsync_WithInvalidLatitude_ThrowsArgumentException(double invalidLat)
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var repo = new WorkLocationRepository(db);
        var useCase = new CreateWorkLocationUseCase(repo);

        var request = new CreateWorkLocationRequest
        {
            Name = "Site A",
            Latitude = invalidLat,
            Longitude = 51.0,
            RadiusInMeters = 50
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, Guid.NewGuid(), Guid.NewGuid()));
        Assert.Contains("عرض جغرافیایی", ex.Message);
    }

    [Theory]
    [InlineData(180.1)]
    [InlineData(-180.1)]
    [InlineData(200.0)]
    public async Task ExecuteAsync_WithInvalidLongitude_ThrowsArgumentException(double invalidLng)
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var repo = new WorkLocationRepository(db);
        var useCase = new CreateWorkLocationUseCase(repo);

        var request = new CreateWorkLocationRequest
        {
            Name = "Site A",
            Latitude = 35.0,
            Longitude = invalidLng,
            RadiusInMeters = 50
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, Guid.NewGuid(), Guid.NewGuid()));
        Assert.Contains("طول جغرافیایی", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task ExecuteAsync_WithInvalidRadius_ThrowsArgumentException(double invalidRadius)
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var repo = new WorkLocationRepository(db);
        var useCase = new CreateWorkLocationUseCase(repo);

        var request = new CreateWorkLocationRequest
        {
            Name = "Site A",
            Latitude = 35.0,
            Longitude = 51.0,
            RadiusInMeters = invalidRadius
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, Guid.NewGuid(), Guid.NewGuid()));
        Assert.Contains("شعاع محدوده", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentOrganization_ThrowsArgumentException()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var repo = new WorkLocationRepository(db);
        var useCase = new CreateWorkLocationUseCase(repo);

        var request = new CreateWorkLocationRequest
        {
            Name = "Site A",
            Latitude = 35.0,
            Longitude = 51.0,
            RadiusInMeters = 50
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, Guid.NewGuid(), Guid.NewGuid()));
        Assert.Contains("سازمان مورد نظر یافت نشد", ex.Message);
    }

    [Fact]
    public async Task Controller_CreateWorkLocation_AsCenterManager_ForOwnOrganization_Succeeds()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var managerOrgId = Guid.NewGuid();
        db.Organizations.Add(new Domain.Entities.Organization
        {
            Id = managerOrgId,
            Name = "Manager Clinic",
            Code = "MC-01",
            Type = OrganizationType.Clinic,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var repo = new WorkLocationRepository(db);
        var useCase = new CreateWorkLocationUseCase(repo);

        var currentUserId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, currentUserId.ToString()),
            new Claim(ClaimTypes.Role, "CenterManager"),
            new Claim("organization_id", managerOrgId.ToString())
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var currentUserService = new CurrentUserService(httpContextAccessor);

        var controller = new WorkLocationsController(useCase, currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var request = new CreateWorkLocationRequest
        {
            Name = "Branchless Location",
            Latitude = 35.7000,
            Longitude = 51.4000,
            RadiusInMeters = 75,
            OrganizationId = managerOrgId // matches token claim
        };

        // Act
        var actionResult = await controller.CreateWorkLocation(request, CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);

        var dto = Assert.IsType<WorkLocationDto>(objectResult.Value);
        Assert.Equal(managerOrgId, dto.OrganizationId);
        Assert.Equal("Branchless Location", dto.Name);
    }

    [Fact]
    public async Task Controller_CreateWorkLocation_AsCenterManager_AttemptingAnotherOrganization_ReturnsForbid()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var managerOrgId = Guid.NewGuid();
        var targetOrgId = Guid.NewGuid(); // different organization

        var repo = new WorkLocationRepository(db);
        var useCase = new CreateWorkLocationUseCase(repo);

        var currentUserId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, currentUserId.ToString()),
            new Claim(ClaimTypes.Role, "CenterManager"),
            new Claim("organization_id", managerOrgId.ToString())
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var currentUserService = new CurrentUserService(httpContextAccessor);

        var controller = new WorkLocationsController(useCase, currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var request = new CreateWorkLocationRequest
        {
            Name = "Malicious Location Request",
            Latitude = 35.7000,
            Longitude = 51.4000,
            RadiusInMeters = 75,
            OrganizationId = targetOrgId // attempting another organization
        };

        // Act
        var actionResult = await controller.CreateWorkLocation(request, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(actionResult);
    }

    [Fact]
    public async Task Controller_CreateWorkLocation_AsSystemAdmin_CreatesForSpecifiedOrganization()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var targetOrgId = Guid.NewGuid();
        db.Organizations.Add(new Domain.Entities.Organization
        {
            Id = targetOrgId,
            Name = "Admin Target Org",
            Code = "ATO-01",
            Type = OrganizationType.Clinic,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var repo = new WorkLocationRepository(db);
        var useCase = new CreateWorkLocationUseCase(repo);

        var currentUserId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, currentUserId.ToString()),
            new Claim(ClaimTypes.Role, "SystemAdmin")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var currentUserService = new CurrentUserService(httpContextAccessor);

        var controller = new WorkLocationsController(useCase, currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var request = new CreateWorkLocationRequest
        {
            Name = "Admin Work Location",
            Latitude = 36.0000,
            Longitude = 52.0000,
            RadiusInMeters = 200,
            OrganizationId = targetOrgId
        };

        // Act
        var actionResult = await controller.CreateWorkLocation(request, CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);

        var dto = Assert.IsType<WorkLocationDto>(objectResult.Value);
        Assert.Equal(targetOrgId, dto.OrganizationId);
    }

    [Fact]
    public void WorkLocation_ModelDoesNotContainBranchIdOrBranchProperty()
    {
        // Arrange & Act
        var entityType = typeof(WorkLocation);
        var branchIdProp = entityType.GetProperty("BranchId");
        var branchProp = entityType.GetProperty("Branch");

        // Assert
        Assert.Null(branchIdProp);
        Assert.Null(branchProp);
    }

    [Fact]
    public void Controller_HasAuthorizeAttribute_RestrictedToCenterManagerAndSystemAdmin()
    {
        // Arrange & Act
        var controllerType = typeof(WorkLocationsController);
        var authorizeAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        // Assert
        Assert.NotNull(authorizeAttr);
        Assert.Equal("CenterManager,SystemAdmin", authorizeAttr.Roles);
    }
}

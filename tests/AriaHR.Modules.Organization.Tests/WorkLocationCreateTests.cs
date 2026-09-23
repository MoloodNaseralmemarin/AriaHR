using System.Reflection;
using System.Security.Claims;
using AriaHR.Modules.Organization.API.Controllers;
using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Options;
using AriaHR.Modules.Organization.Application.UseCases.CreateWorkLocation;
using AriaHR.Modules.Organization.Application.UseCases.GenerateQrCode;
using AriaHR.Modules.Organization.Domain.Entities;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using AriaHR.Modules.Organization.Infrastructure.Repositories;
using AriaHR.Modules.Scheduling.Domain.Entities;
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
        Assert.Equal(35.6892, result.Latitude);
        Assert.Equal(51.3890, result.Longitude);
        Assert.Equal(100, result.RadiusInMeters);
        Assert.True(result.IsActive);
        Assert.Equal(creatorId, result.CreatedByUserId);

        var dbLocation = await db.WorkLocations.FirstOrDefaultAsync(w => w.Id == result.Id);
        Assert.NotNull(dbLocation);
        Assert.Equal(orgId, dbLocation.OrganizationId);
    }

    [Theory]
    [InlineData(90.1)]
    [InlineData(-90.1)]
    [InlineData(100.0)]
    public async Task ExecuteAsync_WithInvalidLatitude_ThrowsArgumentException(double invalidLat)
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var repo = new WorkLocationRepository(db);
        var useCase = new CreateWorkLocationUseCase(repo);

        var request = new CreateWorkLocationRequest
        {
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
            Latitude = 35.0,
            Longitude = 51.0,
            RadiusInMeters = 50
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, Guid.NewGuid(), Guid.NewGuid()));
        Assert.Contains("سازمان مورد نظر یافت نشد", ex.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_DuplicateWorkLocation_ThrowsInvalidOperationException(bool existingIsActive)
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var orgId = Guid.NewGuid();
        db.Organizations.Add(new Domain.Entities.Organization
        {
            Id = orgId,
            Name = "Clinic With Existing WorkLocation",
            Code = "CWE-01",
            Type = OrganizationType.Clinic,
            IsActive = true
        });

        db.WorkLocations.Add(new WorkLocation
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Latitude = 35.7000,
            Longitude = 51.4000,
            RadiusInMeters = 100,
            IsActive = existingIsActive,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var repo = new WorkLocationRepository(db);
        var useCase = new CreateWorkLocationUseCase(repo);

        var request = new CreateWorkLocationRequest
        {
            Latitude = 35.7100,
            Longitude = 51.4100,
            RadiusInMeters = 150,
            IsActive = true
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(request, orgId, Guid.NewGuid()));
        Assert.Equal("برای این سازمان قبلاً محل کار ثبت شده است.", ex.Message);
    }

    [Fact]
    public async Task Controller_CreateWorkLocation_AsCenterManager_ForOwnOrganization_SucceedsAndTakesOrganizationIdFromAuthContext()
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
        var qrRepo = new QrCodeRepository(db);
        var qrUseCase = new GenerateQrCodeUseCase(qrRepo, Microsoft.Extensions.Options.Options.Create(new QrCodeOptions()));

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

        var controller = new WorkLocationsController(useCase, qrUseCase, currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var request = new CreateWorkLocationRequest
        {
            Latitude = 35.7000,
            Longitude = 51.4000,
            RadiusInMeters = 75
        };

        // Act
        var actionResult = await controller.CreateWorkLocation(request, CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);

        var dto = Assert.IsType<WorkLocationDto>(objectResult.Value);
        Assert.Equal(managerOrgId, dto.OrganizationId);
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
        var qrRepo = new QrCodeRepository(db);
        var qrUseCase = new GenerateQrCodeUseCase(qrRepo, Microsoft.Extensions.Options.Options.Create(new QrCodeOptions()));

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

        var controller = new WorkLocationsController(useCase, qrUseCase, currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var request = new CreateWorkLocationRequest
        {
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
        var qrRepo = new QrCodeRepository(db);
        var qrUseCase = new GenerateQrCodeUseCase(qrRepo, Microsoft.Extensions.Options.Options.Create(new QrCodeOptions()));

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

        var controller = new WorkLocationsController(useCase, qrUseCase, currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var request = new CreateWorkLocationRequest
        {
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
    public void WorkLocation_ModelDoesNotContainBranchIdOrBranchPropertyOrNameOrAddress()
    {
        // Arrange & Act
        var entityType = typeof(WorkLocation);

        // Assert
        Assert.Null(entityType.GetProperty("BranchId"));
        Assert.Null(entityType.GetProperty("Branch"));
        Assert.Null(entityType.GetProperty("Name"));
        Assert.Null(entityType.GetProperty("Address"));
    }

    [Fact]
    public void Shift_CreationAndModelIsCompletelyIndependentFromWorkLocation()
    {
        // Arrange & Act
        var shiftType = typeof(Shift);
        var workLocationType = typeof(WorkLocation);

        // Assert
        Assert.Null(shiftType.GetProperty("WorkLocationId"));
        Assert.Null(shiftType.GetProperty("WorkLocation"));
        Assert.Null(workLocationType.GetProperty("ShiftId"));
        Assert.Null(workLocationType.GetProperty("Shift"));

        var shift = new Shift
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Name = "Morning Shift",
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(16, 0)
        };
        Assert.NotNull(shift);
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

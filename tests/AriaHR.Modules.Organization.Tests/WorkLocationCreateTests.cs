using System.Reflection;
using System.Security.Claims;
using AriaHR.Modules.Organization.API.Controllers;
using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Options;
using AriaHR.Modules.Organization.Application.Services;
using AriaHR.Modules.Organization.Application.UseCases.CreateWorkLocation;
using AriaHR.Modules.Organization.Application.UseCases.GetWorkLocations;
using AriaHR.Modules.Organization.Application.UseCases.QrCode;
using AriaHR.Modules.Organization.Application.UseCases.UpdateWorkLocation;
using AriaHR.Modules.Organization.Domain.Entities;
using AriaHR.Modules.Organization.Domain.Enums;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using AriaHR.Modules.Organization.Infrastructure.Repositories;
using AriaHR.Modules.Organization.Infrastructure.Security;
using AriaHR.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace AriaHR.Modules.Organization.Tests;

public class WorkLocationCreateTests
{
    private static OrganizationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new OrganizationDbContext(options);
    }

    private static IOptions<QrCodeOptions> GetOptions() =>
        Options.Create(new QrCodeOptions { SigningKey = "unit-test-signing-key-unit-test-signing-key" });

    private static IQrCodeTokenService GetTokenService(IOptions<QrCodeOptions>? options = null) =>
        new QrCodeTokenService(options ?? GetOptions(), new ConfigurationBuilder().Build());

    private static WorkLocationsController CreateController(
        OrganizationDbContext db,
        ICurrentUserService currentUserService,
        IOptions<QrCodeOptions>? options = null)
    {
        var qrOptions = options ?? GetOptions();
        var workLocationRepository = new WorkLocationRepository(db);
        var qrCodeRepository = new QrCodeRepository(db);

        return new WorkLocationsController(
            new CreateWorkLocationUseCase(workLocationRepository, qrOptions),
            new UpdateWorkLocationUseCase(workLocationRepository, qrOptions),
            new GetWorkLocationsUseCase(workLocationRepository),
            new QrCodeManagementUseCase(workLocationRepository, qrCodeRepository, GetTokenService(qrOptions), qrOptions),
            currentUserService);
    }

    private static ClaimsPrincipal BuildPrincipal(Guid userId, string role, Guid? organizationId = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role)
        };

        if (organizationId.HasValue)
        {
            claims.Add(new Claim("organization_id", organizationId.Value.ToString()));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    private static async Task<Guid> SeedOrganizationAsync(OrganizationDbContext db)
    {
        var orgId = Guid.NewGuid();
        db.Organizations.Add(new Domain.Entities.Organization
        {
            Id = orgId,
            Name = "Tehran Main Clinic",
            Code = "TMC-01",
            OrganizationType = OrganizationType.Clinic,
            IsActive = true
        });

        await db.SaveChangesAsync();
        return orgId;
    }

    private static CreateWorkLocationRequest CreateRequest(decimal radius = 5m, string name = "Main Site") => new()
    {
        Name = name,
        Latitude = 35.689200m,
        Longitude = 51.389000m,
        AllowedRadiusInMeters = radius,
        IsActive = true,
        QrCodeMode = QrCodeMode.Static,
        QrCodeEnabled = true
    };

    [Fact]
    public async Task ExecuteAsync_WithValidRequest_PersistsAllConfiguration()
    {
        using var db = GetInMemoryDbContext();
        var orgId = await SeedOrganizationAsync(db);
        var useCase = new CreateWorkLocationUseCase(new WorkLocationRepository(db), GetOptions());

        var result = await useCase.ExecuteAsync(CreateRequest(), orgId, Guid.NewGuid());

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(orgId, result.OrganizationId);
        Assert.Equal("Main Site", result.Name);
        Assert.Equal(35.689200m, result.Latitude);
        Assert.Equal(51.389000m, result.Longitude);
        Assert.Equal(5m, result.AllowedRadiusInMeters);
        Assert.True(result.QrCodeEnabled);
        Assert.Equal(5, result.QrRotationIntervalMinutes);

        var stored = await db.WorkLocations.SingleAsync(w => w.Id == result.Id);
        Assert.Equal(35.689200m, stored.Latitude);
        Assert.Equal(51.389000m, stored.Longitude);
        Assert.Equal(5m, stored.AllowedRadiusInMeters);
        Assert.False(string.IsNullOrWhiteSpace(stored.QrCodeIdentifier));
    }

    [Fact]
    public async Task ExecuteAsync_MultipleWorkLocationsPerOrganization_AreAllowed()
    {
        using var db = GetInMemoryDbContext();
        var orgId = await SeedOrganizationAsync(db);
        var useCase = new CreateWorkLocationUseCase(new WorkLocationRepository(db), GetOptions());

        var first = await useCase.ExecuteAsync(CreateRequest(5m, "Site A"), orgId, Guid.NewGuid());
        var second = await useCase.ExecuteAsync(CreateRequest(20m, "Site B"), orgId, Guid.NewGuid());

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(5m, first.AllowedRadiusInMeters);
        Assert.Equal(20m, second.AllowedRadiusInMeters);
        Assert.Equal(2, await db.WorkLocations.CountAsync(w => w.OrganizationId == orgId));
    }

    [Theory]
    [InlineData(90.1)]
    [InlineData(-90.1)]
    public async Task ExecuteAsync_InvalidLatitude_ThrowsArgumentException(double latitude)
    {
        using var db = GetInMemoryDbContext();
        var orgId = await SeedOrganizationAsync(db);
        var useCase = new CreateWorkLocationUseCase(new WorkLocationRepository(db), GetOptions());
        var request = CreateRequest();
        request.Latitude = (decimal)latitude;

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, orgId, Guid.NewGuid()));
        Assert.Contains("عرض جغرافیایی", ex.Message);
    }

    [Theory]
    [InlineData(180.5)]
    [InlineData(-180.5)]
    public async Task ExecuteAsync_InvalidLongitude_ThrowsArgumentException(double longitude)
    {
        using var db = GetInMemoryDbContext();
        var orgId = await SeedOrganizationAsync(db);
        var useCase = new CreateWorkLocationUseCase(new WorkLocationRepository(db), GetOptions());
        var request = CreateRequest();
        request.Longitude = (decimal)longitude;

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, orgId, Guid.NewGuid()));
        Assert.Contains("طول جغرافیایی", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(5000)]
    public async Task ExecuteAsync_InvalidRadius_ThrowsArgumentException(double radius)
    {
        using var db = GetInMemoryDbContext();
        var orgId = await SeedOrganizationAsync(db);
        var useCase = new CreateWorkLocationUseCase(new WorkLocationRepository(db), GetOptions());
        var request = CreateRequest();
        request.AllowedRadiusInMeters = (decimal)radius;

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, orgId, Guid.NewGuid()));
        Assert.Contains("شعاع", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_MissingName_ThrowsArgumentException()
    {
        using var db = GetInMemoryDbContext();
        var orgId = await SeedOrganizationAsync(db);
        var useCase = new CreateWorkLocationUseCase(new WorkLocationRepository(db), GetOptions());
        var request = CreateRequest();
        request.Name = "  ";

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, orgId, Guid.NewGuid()));
        Assert.Contains("نام محل کار", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_NonExistentOrganization_ThrowsArgumentException()
    {
        using var db = GetInMemoryDbContext();
        var useCase = new CreateWorkLocationUseCase(new WorkLocationRepository(db), GetOptions());

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ExecuteAsync(CreateRequest(), Guid.NewGuid(), Guid.NewGuid()));
        Assert.Contains("سازمان مورد نظر یافت نشد", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_ChangesCoordinatesAndRadius_WithoutTouchingQrIdentifier()
    {
        using var db = GetInMemoryDbContext();
        var orgId = await SeedOrganizationAsync(db);
        var repository = new WorkLocationRepository(db);
        var createUseCase = new CreateWorkLocationUseCase(repository, GetOptions());
        var updateUseCase = new UpdateWorkLocationUseCase(repository, GetOptions());

        var created = await createUseCase.ExecuteAsync(CreateRequest(5m, "Site A"), orgId, Guid.NewGuid());
        var originalIdentifier = (await db.WorkLocations.SingleAsync(w => w.Id == created.Id)).QrCodeIdentifier;

        var updated = await updateUseCase.ExecuteAsync(created.Id, new UpdateWorkLocationRequest
        {
            Name = "Site A - Updated",
            Latitude = 35.700000m,
            Longitude = 51.400000m,
            AllowedRadiusInMeters = 25m,
            IsActive = true
        }, Guid.NewGuid(), orgId);

        Assert.Equal("Site A - Updated", updated.Name);
        Assert.Equal(35.700000m, updated.Latitude);
        Assert.Equal(25m, updated.AllowedRadiusInMeters);

        var stored = await db.WorkLocations.SingleAsync(w => w.Id == created.Id);
        Assert.Equal(originalIdentifier, stored.QrCodeIdentifier);
        Assert.NotNull(stored.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateAsync_CenterManagerFromAnotherOrganization_ThrowsUnauthorizedAccessException()
    {
        using var db = GetInMemoryDbContext();
        var orgId = await SeedOrganizationAsync(db);
        var repository = new WorkLocationRepository(db);
        var created = await new CreateWorkLocationUseCase(repository, GetOptions())
            .ExecuteAsync(CreateRequest(), orgId, Guid.NewGuid());
        var updateUseCase = new UpdateWorkLocationUseCase(repository, GetOptions());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            updateUseCase.ExecuteAsync(created.Id, new UpdateWorkLocationRequest
            {
                Name = "Hijacked",
                Latitude = 1m,
                Longitude = 1m,
                AllowedRadiusInMeters = 10m,
                IsActive = true
            }, Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task Controller_CreateWorkLocation_AsCenterManager_TakesOrganizationFromClaims()
    {
        using var db = GetInMemoryDbContext();
        var orgId = await SeedOrganizationAsync(db);
        var userId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext { User = BuildPrincipal(userId, "CenterManager", orgId) };
        var currentUser = new CurrentUserService(new HttpContextAccessor { HttpContext = httpContext });

        var controller = CreateController(db, currentUser);
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.CreateWorkLocation(CreateRequest(20m, "Manager Site"), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
        var dto = Assert.IsType<WorkLocationDto>(objectResult.Value);
        Assert.Equal(orgId, dto.OrganizationId);
    }

    [Fact]
    public async Task Controller_CreateWorkLocation_AsCenterManager_ForAnotherOrganization_ReturnsForbid()
    {
        using var db = GetInMemoryDbContext();
        var managerOrgId = await SeedOrganizationAsync(db);
        var httpContext = new DefaultHttpContext { User = BuildPrincipal(Guid.NewGuid(), "CenterManager", managerOrgId) };
        var currentUser = new CurrentUserService(new HttpContextAccessor { HttpContext = httpContext });

        var controller = CreateController(db, currentUser);
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var request = CreateRequest();
        request.OrganizationId = Guid.NewGuid();

        var result = await controller.CreateWorkLocation(request, CancellationToken.None);
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Controller_GetWorkLocation_ForAnotherOrganization_ReturnsForbid()
    {
        using var db = GetInMemoryDbContext();
        var ownerOrgId = await SeedOrganizationAsync(db);
        var created = await new CreateWorkLocationUseCase(new WorkLocationRepository(db), GetOptions())
            .ExecuteAsync(CreateRequest(), ownerOrgId, Guid.NewGuid());

        var managerOrgId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext { User = BuildPrincipal(Guid.NewGuid(), "CenterManager", managerOrgId) };
        var currentUser = new CurrentUserService(new HttpContextAccessor { HttpContext = httpContext });

        var controller = CreateController(db, currentUser);
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.GetWorkLocation(created.Id, CancellationToken.None);
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public void Controller_HasAuthorizeAttribute_RestrictedToCenterManagerAndSystemAdmin()
    {
        var authorizeAttr = typeof(WorkLocationsController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorizeAttr);
        Assert.Equal("CenterManager,SystemAdmin", authorizeAttr.Roles);
    }

    [Fact]
    public void WorkLocation_ExposesRadiusAndQrConfiguration()
    {
        var entityType = typeof(WorkLocation);

        Assert.NotNull(entityType.GetProperty("AllowedRadiusInMeters"));
        Assert.NotNull(entityType.GetProperty("QrCodeMode"));
        Assert.NotNull(entityType.GetProperty("QrCodeIdentifier"));
        Assert.NotNull(entityType.GetProperty("QrRotationIntervalMinutes"));
    }
}

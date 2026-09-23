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
using AriaHR.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace AriaHR.Modules.Organization.Tests;

public class DynamicQrCodeTests
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
    public async Task ExecuteAsync_CenterManagerForOwnOrganization_GeneratesQrCodeSuccessfully()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var managerId = Guid.NewGuid();

        db.Organizations.Add(new Domain.Entities.Organization
        {
            Id = orgId,
            Name = "Tehran Medical Center",
            Code = "TMC-101",
            Type = OrganizationType.Clinic,
            IsActive = true
        });

        db.WorkLocations.Add(new WorkLocation
        {
            Id = locationId,
            OrganizationId = orgId,
            Latitude = 35.6892,
            Longitude = 51.3890,
            RadiusInMeters = 100,
            IsActive = true
        });

        await db.SaveChangesAsync();

        var repository = new QrCodeRepository(db);
        var options = Options.Create(new QrCodeOptions { ExpirationMinutes = 10 });
        var useCase = new GenerateQrCodeUseCase(repository, options);

        // Act
        var result = await useCase.ExecuteAsync(locationId, managerId, orgId);

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Code));
        Assert.True(result.ExpiresAtUtc > DateTime.UtcNow);

        var dbQrCode = await db.QRCodes.FirstOrDefaultAsync(q => q.WorkLocationId == locationId && q.IsActive);
        Assert.NotNull(dbQrCode);
        Assert.Equal(result.Code, dbQrCode.Code);
        Assert.Equal(orgId, dbQrCode.OrganizationId);
        Assert.Equal("Dynamic Attendance QR", dbQrCode.Title);
        Assert.Equal(managerId, dbQrCode.CreatedBy);
    }

    [Fact]
    public async Task ExecuteAsync_CrossOrganizationAccessAttempt_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var ownerOrgId = Guid.NewGuid();
        var otherOrgId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        db.WorkLocations.Add(new WorkLocation
        {
            Id = locationId,
            OrganizationId = ownerOrgId,
            Latitude = 35.6892,
            Longitude = 51.3890,
            RadiusInMeters = 100,
            IsActive = true
        });

        await db.SaveChangesAsync();

        var repository = new QrCodeRepository(db);
        var options = Options.Create(new QrCodeOptions());
        var useCase = new GenerateQrCodeUseCase(repository, options);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            useCase.ExecuteAsync(locationId, Guid.NewGuid(), otherOrgId));
    }

    [Fact]
    public async Task ExecuteAsync_InactiveWorkLocation_ThrowsInvalidOperationException()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        db.WorkLocations.Add(new WorkLocation
        {
            Id = locationId,
            OrganizationId = orgId,
            Latitude = 35.6892,
            Longitude = 51.3890,
            RadiusInMeters = 100,
            IsActive = false // Inactive
        });

        await db.SaveChangesAsync();

        var repository = new QrCodeRepository(db);
        var options = Options.Create(new QrCodeOptions());
        var useCase = new GenerateQrCodeUseCase(repository, options);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.ExecuteAsync(locationId, Guid.NewGuid(), orgId));

        Assert.Contains("غیرفعال است", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_DeletedWorkLocation_ThrowsKeyNotFoundException()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        db.WorkLocations.Add(new WorkLocation
        {
            Id = locationId,
            OrganizationId = orgId,
            Latitude = 35.6892,
            Longitude = 51.3890,
            RadiusInMeters = 100,
            IsActive = true,
            IsDeleted = true // Deleted
        });

        await db.SaveChangesAsync();

        var repository = new QrCodeRepository(db);
        var options = Options.Create(new QrCodeOptions());
        var useCase = new GenerateQrCodeUseCase(repository, options);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            useCase.ExecuteAsync(locationId, Guid.NewGuid(), orgId));
    }

    [Fact]
    public async Task ExecuteAsync_DeactivatesPreviousActiveQrCodesForSameWorkLocation()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        db.WorkLocations.Add(new WorkLocation
        {
            Id = locationId,
            OrganizationId = orgId,
            Latitude = 35.6892,
            Longitude = 51.3890,
            RadiusInMeters = 100,
            IsActive = true
        });

        var oldQrCode = new QRCode
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            WorkLocationId = locationId,
            Code = "old-token-value-12345",
            Title = "Dynamic Attendance QR",
            ValidFrom = DateTime.UtcNow.AddMinutes(-10),
            ValidTo = DateTime.UtcNow.AddMinutes(-5),
            IsActive = true,
            CreatedBy = Guid.NewGuid()
        };

        db.QRCodes.Add(oldQrCode);
        await db.SaveChangesAsync();

        var repository = new QrCodeRepository(db);
        var options = Options.Create(new QrCodeOptions { ExpirationMinutes = 5 });
        var useCase = new GenerateQrCodeUseCase(repository, options);

        // Act
        var result = await useCase.ExecuteAsync(locationId, Guid.NewGuid(), orgId);

        // Assert
        var oldQrInDb = await db.QRCodes.FirstAsync(q => q.Id == oldQrCode.Id);
        Assert.False(oldQrInDb.IsActive);

        var activeQrsInDb = await db.QRCodes.Where(q => q.WorkLocationId == locationId && q.IsActive).ToListAsync();
        Assert.Single(activeQrsInDb);
        Assert.Equal(result.Code, activeQrsInDb.First().Code);
    }

    [Fact]
    public async Task ExecuteAsync_GeneratesSecureUnpredictableToken()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        db.WorkLocations.Add(new WorkLocation
        {
            Id = locationId,
            OrganizationId = orgId,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var repository = new QrCodeRepository(db);
        var options = Options.Create(new QrCodeOptions());
        var useCase = new GenerateQrCodeUseCase(repository, options);

        // Act
        var result1 = await useCase.ExecuteAsync(locationId, Guid.NewGuid(), orgId);
        var result2 = await useCase.ExecuteAsync(locationId, Guid.NewGuid(), orgId);

        // Assert
        Assert.NotEqual(result1.Code, result2.Code);
        Assert.Equal(64, result1.Code.Length); // 32 bytes = 64 hex chars
        Assert.False(Guid.TryParse(result1.Code, out _));
    }

    [Fact]
    public async Task Controller_GenerateQrCode_AsCenterManager_ForOwnOrganization_Succeeds()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.WorkLocations.Add(new WorkLocation
        {
            Id = locationId,
            OrganizationId = orgId,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var workLocationRepo = new WorkLocationRepository(db);
        var createUseCase = new CreateWorkLocationUseCase(workLocationRepo);
        var qrRepo = new QrCodeRepository(db);
        var qrUseCase = new GenerateQrCodeUseCase(qrRepo, Options.Create(new QrCodeOptions()));

        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "CenterManager"),
            new Claim("organization_id", orgId.ToString())
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var currentUserService = new CurrentUserService(httpContextAccessor);

        var controller = new WorkLocationsController(createUseCase, qrUseCase, currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        // Act
        var actionResult = await controller.GenerateQrCode(locationId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var dto = Assert.IsType<QrCodeResponse>(okResult.Value);
        Assert.False(string.IsNullOrWhiteSpace(dto.Code));
        Assert.True(dto.ExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task Controller_GenerateQrCode_AsCenterManager_CrossOrganizationAttempt_ReturnsForbid()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var ownerOrgId = Guid.NewGuid();
        var managerOrgId = Guid.NewGuid(); // different org
        var locationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.WorkLocations.Add(new WorkLocation
        {
            Id = locationId,
            OrganizationId = ownerOrgId,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var workLocationRepo = new WorkLocationRepository(db);
        var createUseCase = new CreateWorkLocationUseCase(workLocationRepo);
        var qrRepo = new QrCodeRepository(db);
        var qrUseCase = new GenerateQrCodeUseCase(qrRepo, Options.Create(new QrCodeOptions()));

        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "CenterManager"),
            new Claim("organization_id", managerOrgId.ToString())
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var currentUserService = new CurrentUserService(httpContextAccessor);

        var controller = new WorkLocationsController(createUseCase, qrUseCase, currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        // Act
        var actionResult = await controller.GenerateQrCode(locationId, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(actionResult);
    }

    [Fact]
    public async Task Controller_GenerateQrCode_AsSystemAdmin_GeneratesForAnyValidWorkLocation()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.WorkLocations.Add(new WorkLocation
        {
            Id = locationId,
            OrganizationId = orgId,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var workLocationRepo = new WorkLocationRepository(db);
        var createUseCase = new CreateWorkLocationUseCase(workLocationRepo);
        var qrRepo = new QrCodeRepository(db);
        var qrUseCase = new GenerateQrCodeUseCase(qrRepo, Options.Create(new QrCodeOptions()));

        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "SystemAdmin")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var currentUserService = new CurrentUserService(httpContextAccessor);

        var controller = new WorkLocationsController(createUseCase, qrUseCase, currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        // Act
        var actionResult = await controller.GenerateQrCode(locationId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var dto = Assert.IsType<QrCodeResponse>(okResult.Value);
        Assert.False(string.IsNullOrWhiteSpace(dto.Code));
    }

    [Fact]
    public void Controller_GenerateQrCodeMethod_HasCorrectAttributes()
    {
        // Arrange & Act
        var methodInfo = typeof(WorkLocationsController).GetMethod("GenerateQrCode");

        // Assert
        Assert.NotNull(methodInfo);
        var httpPostAttr = methodInfo.GetCustomAttribute<HttpPostAttribute>();
        Assert.NotNull(httpPostAttr);
        Assert.Equal("{workLocationId}/qr", httpPostAttr.Template);
    }
}

using System.Reflection;
using System.Security.Claims;
using AriaHR.Modules.Organization.API.Controllers;
using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Exceptions;
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
using AriaHR.Modules.Organization.Infrastructure.Services;
using AriaHR.Shared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace AriaHR.Modules.Organization.Tests;

public class DynamicQrCodeTests
{
    private sealed record TestContext(
        OrganizationDbContext Db,
        Guid OrganizationId,
        Guid WorkLocationId);

    private static IOptions<QrCodeOptions> OptionsValue(int interval = 5) =>
        Options.Create(new QrCodeOptions
        {
            SigningKey = "unit-test-signing-key-unit-test-signing-key",
            DefaultRotationIntervalMinutes = interval,
            MinRotationIntervalMinutes = 1,
            MaxRotationIntervalMinutes = 60
        });

    private static QrCodeTokenService CreateTokenService(IOptions<QrCodeOptions>? options = null) =>
        new(options ?? OptionsValue(), new ConfigurationBuilder().Build());

    private sealed class FakeTimeProvider : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }

    private static QrCodeManagementUseCase CreateManagementUseCase(
        OrganizationDbContext db,
        IOptions<QrCodeOptions>? options = null,
        TimeProvider? timeProvider = null)
    {
        var qrOptions = options ?? OptionsValue();
        return new QrCodeManagementUseCase(
            new WorkLocationRepository(db),
            new QrCodeRepository(db),
            CreateTokenService(qrOptions),
            qrOptions,
            timeProvider);
    }

    private static WorkLocationLookupService CreateLookupService(
        OrganizationDbContext db,
        IOptions<QrCodeOptions>? options = null)
    {
        return new WorkLocationLookupService(db, CreateTokenService(options));
    }

    private static async Task<TestContext> SeedAsync(
        QrCodeMode mode,
        bool enabled = true,
        int interval = 5,
        bool isActive = true)
    {
        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new OrganizationDbContext(options);
        var orgId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        db.Organizations.Add(new Domain.Entities.Organization
        {
            Id = orgId,
            Name = "Clinic",
            Code = "C-01",
            OrganizationType = OrganizationType.Clinic,
            IsActive = true
        });

        db.WorkLocations.Add(new WorkLocation
        {
            Id = locationId,
            OrganizationId = orgId,
            Name = "Main Site",
            Latitude = 35.689200m,
            Longitude = 51.389000m,
            AllowedRadiusInMeters = 5m,
            IsActive = isActive,
            QrCodeMode = mode,
            QrCodeIdentifier = "0123456789abcdef0123456789abcdef",
            QrCodeEnabled = enabled,
            QrRotationIntervalMinutes = interval,
            CreatedAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return new TestContext(db, orgId, locationId);
    }

    [Fact]
    public async Task StaticQr_IsStableAndHasNoExpiry()
    {
        var ctx = await SeedAsync(QrCodeMode.Static);
        using var _ = ctx.Db;
        var useCase = CreateManagementUseCase(ctx.Db);

        var first = await useCase.GenerateAsync(ctx.WorkLocationId, Guid.NewGuid(), ctx.OrganizationId);
        var second = await useCase.GetCurrentAsync(ctx.WorkLocationId, ctx.OrganizationId);

        Assert.Equal("Static", first.QrCodeMode);
        Assert.Null(first.ExpiresAtUtc);
        Assert.Equal(first.Token, second.Token);

        var validation = await CreateLookupService(ctx.Db)
            .ValidateQrCodeAsync(first.Token, ctx.OrganizationId);
        Assert.True(validation.IsValid);
        Assert.Equal(ctx.WorkLocationId, validation.WorkLocationId);
    }

    [Fact]
    public async Task DynamicQr_ReturnsSameTokenWithinRotationWindow()
    {
        var ctx = await SeedAsync(QrCodeMode.Dynamic, interval: 5);
        using var _ = ctx.Db;
        var clock = new FakeTimeProvider { UtcNow = new DateTimeOffset(2026, 11, 15, 10, 2, 30, TimeSpan.Zero) };
        var useCase = CreateManagementUseCase(ctx.Db, timeProvider: clock);

        var first = await useCase.GetCurrentAsync(ctx.WorkLocationId, ctx.OrganizationId);
        clock.UtcNow = clock.UtcNow.AddSeconds(20);
        var second = await useCase.GetCurrentAsync(ctx.WorkLocationId, ctx.OrganizationId);

        Assert.Equal("Dynamic", first.QrCodeMode);
        Assert.NotNull(first.ExpiresAtUtc);
        Assert.Equal(new DateTime(2026, 11, 15, 10, 5, 0, DateTimeKind.Utc), first.ExpiresAtUtc);
        Assert.Equal(first.Token, second.Token);
        Assert.Equal(first.ExpiresAtUtc, second.RefreshAfterUtc);
        Assert.Equal(first.ExpiresAtUtc, second.ExpiresAtUtc);
    }

    [Fact]
    public async Task DynamicQr_RotatesAtWindowBoundary_AndPreviousTokenExpires()
    {
        var ctx = await SeedAsync(QrCodeMode.Dynamic, interval: 5);
        using var _ = ctx.Db;
        var clock = new FakeTimeProvider { UtcNow = new DateTimeOffset(2026, 11, 15, 10, 2, 30, TimeSpan.Zero) };
        var useCase = CreateManagementUseCase(ctx.Db, timeProvider: clock);
        var lookup = CreateLookupService(ctx.Db);

        var beforeRotation = await useCase.GetCurrentAsync(ctx.WorkLocationId, ctx.OrganizationId);

        clock.UtcNow = clock.UtcNow.AddMinutes(5);
        var afterRotation = await useCase.GetCurrentAsync(ctx.WorkLocationId, ctx.OrganizationId);

        Assert.NotEqual(beforeRotation.Token, afterRotation.Token);

        var oldTokenValidation = await lookup.ValidateQrCodeAsync(
            beforeRotation.Token,
            ctx.OrganizationId,
            clock.UtcNow.UtcDateTime);

        var newTokenValidation = await lookup.ValidateQrCodeAsync(
            afterRotation.Token,
            ctx.OrganizationId,
            clock.UtcNow.UtcDateTime);

        Assert.False(oldTokenValidation.IsValid);
        Assert.Equal("QR_CODE_EXPIRED", oldTokenValidation.ErrorCode);
        Assert.True(newTokenValidation.IsValid);
    }

    [Fact]
    public async Task DynamicQr_ExpiredToken_IsRejected()
    {
        var ctx = await SeedAsync(QrCodeMode.Dynamic, interval: 1);
        using var _ = ctx.Db;

        var tokenService = CreateTokenService(OptionsValue(1));
        var issued = tokenService.Issue(
            ctx.WorkLocationId,
            "0123456789abcdef0123456789abcdef",
            QrCodeMode.Dynamic,
            1,
            DateTime.UtcNow.AddMinutes(-3));

        var validation = await CreateLookupService(ctx.Db, OptionsValue(1))
            .ValidateQrCodeAsync(issued.Token, ctx.OrganizationId);

        Assert.False(validation.IsValid);
        Assert.Equal("QR_CODE_EXPIRED", validation.ErrorCode);
    }

    [Fact]
    public async Task TamperedToken_IsRejected()
    {
        var ctx = await SeedAsync(QrCodeMode.Static);
        using var _ = ctx.Db;
        var current = await CreateManagementUseCase(ctx.Db).GetCurrentAsync(ctx.WorkLocationId, ctx.OrganizationId);

        char last = current.Token[^1];
        char replacement = last == 'A' ? 'B' : 'A';
        string tampered = current.Token[..^1] + replacement;

        var validation = await CreateLookupService(ctx.Db)
            .ValidateQrCodeAsync(tampered, ctx.OrganizationId);

        Assert.False(validation.IsValid);
        Assert.Equal("QR_CODE_INVALID", validation.ErrorCode);
    }

    [Fact]
    public async Task TokenFromAnotherWorkLocation_IsRejected()
    {
        var ctx = await SeedAsync(QrCodeMode.Static);
        using var _ = ctx.Db;

        var otherLocationId = Guid.NewGuid();
        var otherOrgId = Guid.NewGuid();
        ctx.Db.WorkLocations.Add(new WorkLocation
        {
            Id = otherLocationId,
            OrganizationId = otherOrgId,
            Name = "Other Site",
            Latitude = 35.7m,
            Longitude = 51.4m,
            AllowedRadiusInMeters = 10m,
            IsActive = true,
            QrCodeMode = QrCodeMode.Static,
            QrCodeIdentifier = "ffffffffffffffffffffffffffffffff",
            QrCodeEnabled = true,
            QrRotationIntervalMinutes = 5
        });
        await ctx.Db.SaveChangesAsync();

        // A token signed for the other location must not be usable for the first location.
        var otherToken = CreateTokenService().Issue(
            otherLocationId,
            "ffffffffffffffffffffffffffffffff",
            QrCodeMode.Static,
            5,
            DateTime.UtcNow);

        var validation = await CreateLookupService(ctx.Db)
            .ValidateQrCodeAsync(otherToken.Token, ctx.OrganizationId);

        Assert.False(validation.IsValid);
        Assert.Equal("EMPLOYEE_NOT_AUTHORIZED", validation.ErrorCode);
        Assert.Equal(otherLocationId, validation.WorkLocationId);
    }

    [Fact]
    public async Task DisabledQr_IsRejected()
    {
        var ctx = await SeedAsync(QrCodeMode.Static, enabled: false);
        using var _ = ctx.Db;

        var token = CreateTokenService().Issue(
            ctx.WorkLocationId,
            "0123456789abcdef0123456789abcdef",
            QrCodeMode.Static,
            5,
            DateTime.UtcNow);

        var validation = await CreateLookupService(ctx.Db)
            .ValidateQrCodeAsync(token.Token, ctx.OrganizationId);

        Assert.False(validation.IsValid);
        Assert.Equal("QR_CODE_DISABLED", validation.ErrorCode);
    }

    [Fact]
    public async Task RevokeQr_InvalidatesPreviousStaticToken()
    {
        var ctx = await SeedAsync(QrCodeMode.Static);
        using var _ = ctx.Db;
        var useCase = CreateManagementUseCase(ctx.Db);

        var before = await useCase.GenerateAsync(ctx.WorkLocationId, Guid.NewGuid(), ctx.OrganizationId);
        var revoked = await useCase.RevokeAsync(ctx.WorkLocationId, Guid.NewGuid(), ctx.OrganizationId);
        var after = await useCase.GetCurrentAsync(ctx.WorkLocationId, ctx.OrganizationId);

        Assert.NotEqual(before.Token, after.Token);
        Assert.Equal(revoked.Token, after.Token);

        var oldValidation = await CreateLookupService(ctx.Db).ValidateQrCodeAsync(before.Token, ctx.OrganizationId);
        var newValidation = await CreateLookupService(ctx.Db).ValidateQrCodeAsync(after.Token, ctx.OrganizationId);

        Assert.False(oldValidation.IsValid);
        Assert.Equal("QR_CODE_INVALID", oldValidation.ErrorCode);
        Assert.True(newValidation.IsValid);
    }

    [Fact]
    public async Task MultipleEmployees_CanUseTheSameDynamicToken()
    {
        var ctx = await SeedAsync(QrCodeMode.Dynamic, interval: 5);
        using var _ = ctx.Db;
        var current = await CreateManagementUseCase(ctx.Db).GetCurrentAsync(ctx.WorkLocationId, ctx.OrganizationId);
        var lookup = CreateLookupService(ctx.Db);

        var firstEmployee = await lookup.ValidateQrCodeAsync(current.Token, ctx.OrganizationId);
        var secondEmployee = await lookup.ValidateQrCodeAsync(current.Token, ctx.OrganizationId);

        Assert.True(firstEmployee.IsValid);
        Assert.True(secondEmployee.IsValid);
    }

    [Fact]
    public async Task UpdateSettings_PersistsModeAndInterval()
    {
        var ctx = await SeedAsync(QrCodeMode.Static);
        using var _ = ctx.Db;
        var useCase = CreateManagementUseCase(ctx.Db);

        var settings = await useCase.UpdateSettingsAsync(
            ctx.WorkLocationId,
            new UpdateQrCodeSettingsRequest { QrCodeMode = QrCodeMode.Dynamic, QrRotationIntervalMinutes = 10 },
            Guid.NewGuid(),
            ctx.OrganizationId);

        Assert.Equal("Dynamic", settings.QrCodeMode);
        Assert.Equal(10, settings.RotationIntervalMinutes);

        var stored = await ctx.Db.WorkLocations.SingleAsync(w => w.Id == ctx.WorkLocationId);
        Assert.Equal(QrCodeMode.Dynamic, stored.QrCodeMode);
        Assert.Equal(10, stored.QrRotationIntervalMinutes);
    }

    [Fact]
    public async Task UpdateSettings_InvalidInterval_ThrowsArgumentException()
    {
        var ctx = await SeedAsync(QrCodeMode.Static);
        using var _ = ctx.Db;
        var useCase = CreateManagementUseCase(ctx.Db);

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.UpdateSettingsAsync(
            ctx.WorkLocationId,
            new UpdateQrCodeSettingsRequest { QrCodeMode = QrCodeMode.Dynamic, QrRotationIntervalMinutes = 90 },
            Guid.NewGuid(),
            ctx.OrganizationId));
    }

    [Fact]
    public async Task CrossOrganizationAccess_IsRejected()
    {
        var ctx = await SeedAsync(QrCodeMode.Static);
        using var _ = ctx.Db;
        var useCase = CreateManagementUseCase(ctx.Db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            useCase.GetCurrentAsync(ctx.WorkLocationId, Guid.NewGuid()));
    }

    [Fact]
    public async Task DisableQr_PreventsTokenUse()
    {
        var ctx = await SeedAsync(QrCodeMode.Static);
        using var _ = ctx.Db;
        var useCase = CreateManagementUseCase(ctx.Db);

        var current = await useCase.GenerateAsync(ctx.WorkLocationId, Guid.NewGuid(), ctx.OrganizationId);
        await useCase.SetEnabledAsync(ctx.WorkLocationId, false, Guid.NewGuid(), ctx.OrganizationId);

        var validation = await CreateLookupService(ctx.Db).ValidateQrCodeAsync(current.Token, ctx.OrganizationId);
        Assert.False(validation.IsValid);
        Assert.Equal("QR_CODE_DISABLED", validation.ErrorCode);

        await Assert.ThrowsAsync<QrCodeOperationException>(() =>
            useCase.GetCurrentAsync(ctx.WorkLocationId, ctx.OrganizationId));
    }

    [Fact]
    public async Task Controller_GenerateQrCode_AsCenterManager_ForOwnOrganization_Succeeds()
    {
        var options = OptionsValue();
        var ctx = await SeedAsync(QrCodeMode.Static);
        using var _ = ctx.Db;

        var userId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "CenterManager"),
            new Claim("organization_id", ctx.OrganizationId.ToString())
        }, "TestAuth"));

        var currentUserService = new CurrentUserService(new HttpContextAccessor { HttpContext = httpContext });
        var workLocationRepository = new WorkLocationRepository(ctx.Db);

        var controller = new WorkLocationsController(
            new CreateWorkLocationUseCase(workLocationRepository, options),
            new UpdateWorkLocationUseCase(workLocationRepository, options),
            new GetWorkLocationsUseCase(workLocationRepository),
            new QrCodeManagementUseCase(workLocationRepository, new QrCodeRepository(ctx.Db), CreateTokenService(options), options),
            currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var actionResult = await controller.GenerateQrCode(ctx.WorkLocationId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var dto = Assert.IsType<QrCodeResponse>(okResult.Value);
        Assert.False(string.IsNullOrWhiteSpace(dto.Token));
    }

    [Fact]
    public async Task Controller_GenerateQrCode_CrossOrganization_ReturnsForbid()
    {
        var options = OptionsValue();
        var ctx = await SeedAsync(QrCodeMode.Static);
        using var _ = ctx.Db;

        var managerOrgId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "CenterManager"),
            new Claim("organization_id", managerOrgId.ToString())
        }, "TestAuth"));

        var currentUserService = new CurrentUserService(new HttpContextAccessor { HttpContext = httpContext });
        var workLocationRepository = new WorkLocationRepository(ctx.Db);

        var controller = new WorkLocationsController(
            new CreateWorkLocationUseCase(workLocationRepository, options),
            new UpdateWorkLocationUseCase(workLocationRepository, options),
            new GetWorkLocationsUseCase(workLocationRepository),
            new QrCodeManagementUseCase(workLocationRepository, new QrCodeRepository(ctx.Db), CreateTokenService(options), options),
            currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var actionResult = await controller.GenerateQrCode(ctx.WorkLocationId, CancellationToken.None);
        Assert.IsType<ForbidResult>(actionResult);
    }

    [Fact]
    public void Controller_GenerateQrCodeMethod_HasExpectedRoute()
    {
        var methodInfo = typeof(WorkLocationsController).GetMethod("GenerateQrCode");

        Assert.NotNull(methodInfo);
        var httpPostAttr = methodInfo.GetCustomAttribute<HttpPostAttribute>();
        Assert.NotNull(httpPostAttr);
        Assert.Equal("{workLocationId:guid}/qrcode/generate", httpPostAttr.Template);
    }
}

using System.Reflection;
using System.Security.Claims;
using AriaHR.Modules.Organization.API.Controllers;
using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Options;
using AriaHR.Modules.Organization.Application.Services;
using AriaHR.Modules.Organization.Application.UseCases.CreateWorkLocation;
using AriaHR.Modules.Organization.Application.UseCases.GenerateQrCode;
using AriaHR.Modules.Organization.Application.UseCases.GetQrCode;
using AriaHR.Modules.Organization.Application.UseCases.GetQrCodeSettings;
using AriaHR.Modules.Organization.Application.UseCases.UpdateQrCodeSettings;
using AriaHR.Modules.Organization.Domain.Entities;
using AriaHR.Modules.Organization.Domain.Enums;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using AriaHR.Modules.Organization.Infrastructure.Repositories;
using AriaHR.Modules.Organization.Infrastructure.Services;
using AriaHR.Shared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace AriaHR.Modules.Organization.Tests;

public class DynamicQrCodeTests
{
    private static readonly DateTime AnchorUtc = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    private sealed class MutableTimeProvider : TimeProvider
    {
        public DateTime UtcNow { get; set; } = AnchorUtc;

        public override DateTimeOffset GetUtcNow() => new(UtcNow, TimeSpan.Zero);
    }

    private sealed class QrTestContext
    {
        public QrTestContext(OrganizationDbContext db, MutableTimeProvider clock)
        {
            Db = db;
            Clock = clock;
            Repository = new QrCodeRepository(db);
            Tokens = new QrCodeTokenService();
            var options = Options.Create(new QrCodeOptions { DefaultRotationMinutes = 5 });

            QrCodeService = new QrCodeService(Repository, Tokens, options, clock);
            Generate = new GenerateQrCodeUseCase(QrCodeService);
            Get = new GetQrCodeUseCase(QrCodeService);
            GetSettings = new GetQrCodeSettingsUseCase(Repository, QrCodeService);
            UpdateSettings = new UpdateQrCodeSettingsUseCase(Repository, QrCodeService, Tokens, options, clock);
            Lookup = new WorkLocationLookupService(db, Tokens, clock);
        }

        public OrganizationDbContext Db { get; }
        public MutableTimeProvider Clock { get; }
        public QrCodeRepository Repository { get; }
        public QrCodeTokenService Tokens { get; }
        public IQrCodeService QrCodeService { get; }
        public GenerateQrCodeUseCase Generate { get; }
        public GetQrCodeUseCase Get { get; }
        public GetQrCodeSettingsUseCase GetSettings { get; }
        public UpdateQrCodeSettingsUseCase UpdateSettings { get; }
        public WorkLocationLookupService Lookup { get; }

        public WorkLocationsController CreateController(Guid userId, string role, Guid? organizationId)
        {
            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Role, role)
            };

            if (organizationId.HasValue)
            {
                claims.Add(new Claim("organization_id", organizationId.Value.ToString()));
            }

            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

            var accessor = new HttpContextAccessor { HttpContext = httpContext };
            var currentUserService = new CurrentUserService(accessor);

            return new WorkLocationsController(
                new CreateWorkLocationUseCase(new WorkLocationRepository(Db)),
                Generate,
                Get,
                GetSettings,
                UpdateSettings,
                currentUserService)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };
        }
    }

    private static OrganizationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new OrganizationDbContext(options);
    }

    private static async Task<(Guid OrganizationId, Guid WorkLocationId)> SeedWorkLocationAsync(
        OrganizationDbContext db,
        Guid? organizationId = null,
        QrCodeMode mode = QrCodeMode.Dynamic,
        int rotationMinutes = 5,
        bool verificationEnabled = true,
        bool isActive = true)
    {
        Guid orgId = organizationId ?? Guid.NewGuid();
        Guid locationId = Guid.NewGuid();

        db.Organizations.Add(new Domain.Entities.Organization
        {
            Id = orgId,
            Name = "Tehran Medical Center",
            Code = $"TMC-{orgId:N}",
            OrganizationType = OrganizationType.Clinic,
            IsActive = true,
            CreatedAtUtc = AnchorUtc
        });

        db.WorkLocations.Add(new WorkLocation
        {
            Id = locationId,
            OrganizationId = orgId,
            Latitude = 35.6892,
            Longitude = 51.3890,
            RadiusInMeters = 100,
            IsActive = isActive,
            QrCodeVerificationEnabled = verificationEnabled,
            QrCodeMode = mode,
            QrCodeRotationMinutes = rotationMinutes,
            CreatedAtUtc = AnchorUtc
        });

        await db.SaveChangesAsync();
        return (orgId, locationId);
    }

    [Fact]
    public async Task StaticMode_CodeRemainsUnchangedUntilAdminRegenerates()
    {
        using var db = GetInMemoryDbContext();
        var ctx = new QrTestContext(db, new MutableTimeProvider());
        var (orgId, locationId) = await SeedWorkLocationAsync(db, mode: QrCodeMode.Static);

        var settings = await ctx.UpdateSettings.ExecuteAsync(
            locationId,
            new UpdateQrCodeSettingsRequest { VerificationEnabled = true, Mode = QrCodeMode.Static, RotationMinutes = 5 },
            Guid.NewGuid(),
            orgId);

        Assert.NotNull(settings.CurrentCode);
        Assert.Equal("Static", settings.Mode);

        var first = await ctx.Get.ExecuteAsync(locationId, orgId);
        var second = await ctx.Get.ExecuteAsync(locationId, orgId);

        Assert.Equal(first.Code, second.Code);
        Assert.Equal(settings.CurrentCode, first.Code);
        Assert.Null(first.ExpiresAtUtc);
        Assert.Null(first.RemainingValiditySeconds);

        ctx.Clock.UtcNow = AnchorUtc.AddHours(3);
        var later = await ctx.Get.ExecuteAsync(locationId, orgId);
        Assert.Equal(first.Code, later.Code);
    }

    [Fact]
    public async Task StaticMode_ManualRegeneration_InvalidatesPreviousCode()
    {
        using var db = GetInMemoryDbContext();
        var ctx = new QrTestContext(db, new MutableTimeProvider());
        var (orgId, locationId) = await SeedWorkLocationAsync(db, mode: QrCodeMode.Static);
        var adminId = Guid.NewGuid();

        var first = await ctx.Generate.ExecuteAsync(locationId, adminId, orgId);
        var second = await ctx.Generate.ExecuteAsync(locationId, adminId, orgId);

        Assert.NotNull(first.Code);
        Assert.NotNull(second.Code);
        Assert.NotEqual(first.Code, second.Code);

        var oldValidation = await ctx.Lookup.ValidateQrCodeAsync(first.Code!, orgId);
        Assert.False(oldValidation.IsValid);

        var newValidation = await ctx.Lookup.ValidateQrCodeAsync(second.Code!, orgId);
        Assert.True(newValidation.IsValid);
        Assert.Equal(locationId, newValidation.WorkLocationId);
        Assert.Equal(orgId, newValidation.OrganizationId);
        Assert.NotNull(newValidation.QRCodeId);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(60)]
    public async Task DynamicMode_PredefinedRotationIntervals_AreAccepted(int rotationMinutes)
    {
        using var db = GetInMemoryDbContext();
        var ctx = new QrTestContext(db, new MutableTimeProvider());
        var (orgId, locationId) = await SeedWorkLocationAsync(db);

        var settings = await ctx.UpdateSettings.ExecuteAsync(
            locationId,
            new UpdateQrCodeSettingsRequest
            {
                VerificationEnabled = true,
                Mode = QrCodeMode.Dynamic,
                RotationMinutes = rotationMinutes
            },
            Guid.NewGuid(),
            orgId);

        Assert.Equal(rotationMinutes, settings.RotationMinutes);
        Assert.Equal("Dynamic", settings.Mode);
        Assert.NotNull(settings.CurrentCode);
        Assert.NotNull(settings.ExpiresAtUtc);
        Assert.True(settings.ExpiresAtUtc > ctx.Clock.UtcNow);
        Assert.True(settings.ExpiresAtUtc <= ctx.Clock.UtcNow.AddMinutes(rotationMinutes));
        Assert.True(settings.RemainingValiditySeconds > 0);
        Assert.True(settings.RemainingValiditySeconds <= rotationMinutes * 60);
    }

    [Fact]
    public async Task DynamicMode_CustomRotationInterval_IsSupported()
    {
        using var db = GetInMemoryDbContext();
        var ctx = new QrTestContext(db, new MutableTimeProvider());
        var (orgId, locationId) = await SeedWorkLocationAsync(db);

        var settings = await ctx.UpdateSettings.ExecuteAsync(
            locationId,
            new UpdateQrCodeSettingsRequest { VerificationEnabled = true, Mode = QrCodeMode.Dynamic, RotationMinutes = 7 },
            Guid.NewGuid(),
            orgId);

        Assert.Equal(7, settings.RotationMinutes);
        Assert.True(settings.ExpiresAtUtc <= ctx.Clock.UtcNow.AddMinutes(7));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1441)]
    public async Task DynamicMode_InvalidRotationInterval_IsRejected(int rotationMinutes)
    {
        using var db = GetInMemoryDbContext();
        var ctx = new QrTestContext(db, new MutableTimeProvider());
        var (orgId, locationId) = await SeedWorkLocationAsync(db);

        await Assert.ThrowsAsync<ArgumentException>(() => ctx.UpdateSettings.ExecuteAsync(
            locationId,
            new UpdateQrCodeSettingsRequest { VerificationEnabled = true, Mode = QrCodeMode.Dynamic, RotationMinutes = rotationMinutes },
            Guid.NewGuid(),
            orgId));
    }

    [Fact]
    public async Task DynamicMode_AllClientsReceiveSameCodeWithinWindow_AndItRotatesAfterwards()
    {
        using var db = GetInMemoryDbContext();
        var ctx = new QrTestContext(db, new MutableTimeProvider());
        var (orgId, locationId) = await SeedWorkLocationAsync(db);

        var firstAdminCode = await ctx.Generate.ExecuteAsync(locationId, Guid.NewGuid(), orgId);
        var secondAdminCode = await ctx.Generate.ExecuteAsync(locationId, Guid.NewGuid(), orgId);
        var readCode = await ctx.Get.ExecuteAsync(locationId, orgId);

        Assert.Equal(firstAdminCode.Code, secondAdminCode.Code);
        Assert.Equal(firstAdminCode.Code, readCode.Code);

        var validation = await ctx.Lookup.ValidateQrCodeAsync(firstAdminCode.Code!, orgId);
        Assert.True(validation.IsValid);

        ctx.Clock.UtcNow = AnchorUtc.AddMinutes(5);
        var rotated = await ctx.Get.ExecuteAsync(locationId, orgId);

        Assert.NotEqual(firstAdminCode.Code, rotated.Code);

        var expiredValidation = await ctx.Lookup.ValidateQrCodeAsync(firstAdminCode.Code!, orgId);
        Assert.False(expiredValidation.IsValid);
        Assert.Contains("منقضی", expiredValidation.ErrorMessage);

        var rotatedValidation = await ctx.Lookup.ValidateQrCodeAsync(rotated.Code!, orgId);
        Assert.True(rotatedValidation.IsValid);
    }

    [Fact]
    public async Task DynamicMode_CodeIsValidUntilWindowEnd_AndRejectedAtBoundary()
    {
        using var db = GetInMemoryDbContext();
        var ctx = new QrTestContext(db, new MutableTimeProvider());
        var (orgId, locationId) = await SeedWorkLocationAsync(db, rotationMinutes: 10);

        var code = await ctx.Get.ExecuteAsync(locationId, orgId);
        Assert.NotNull(code.Code);

        ctx.Clock.UtcNow = AnchorUtc.AddMinutes(10).AddSeconds(-1);
        var stillValid = await ctx.Lookup.ValidateQrCodeAsync(code.Code!, orgId);
        Assert.True(stillValid.IsValid);

        ctx.Clock.UtcNow = AnchorUtc.AddMinutes(10);
        var expired = await ctx.Lookup.ValidateQrCodeAsync(code.Code!, orgId);
        Assert.False(expired.IsValid);

        ctx.Clock.UtcNow = AnchorUtc.AddMinutes(9);
        var stillValidAgain = await ctx.Lookup.ValidateQrCodeAsync(code.Code!, orgId);
        Assert.True(stillValidAgain.IsValid);
    }

    [Fact]
    public async Task ExpiredDynamicCode_CannotBeReusedAfterWindowRotates()
    {
        using var db = GetInMemoryDbContext();
        var ctx = new QrTestContext(db, new MutableTimeProvider());
        var (orgId, locationId) = await SeedWorkLocationAsync(db, rotationMinutes: 1);

        var code = await ctx.Get.ExecuteAsync(locationId, orgId);

        ctx.Clock.UtcNow = AnchorUtc.AddMinutes(1);
        var nextCode = await ctx.Get.ExecuteAsync(locationId, orgId);
        Assert.NotEqual(code.Code, nextCode.Code);

        ctx.Clock.UtcNow = AnchorUtc.AddMinutes(5);
        var reuseAttempt = await ctx.Lookup.ValidateQrCodeAsync(code.Code!, orgId);
        Assert.False(reuseAttempt.IsValid);
    }

    [Fact]
    public async Task DisabledVerification_DoesNotIssueOrAcceptQrCodes()
    {
        using var db = GetInMemoryDbContext();
        var ctx = new QrTestContext(db, new MutableTimeProvider());
        var (orgId, locationId) = await SeedWorkLocationAsync(db);

        var issued = await ctx.Generate.ExecuteAsync(locationId, Guid.NewGuid(), orgId);
        Assert.True(await TryValidateAsync(ctx, issued.Code!, orgId));

        await ctx.UpdateSettings.ExecuteAsync(
            locationId,
            new UpdateQrCodeSettingsRequest { VerificationEnabled = false, Mode = QrCodeMode.Dynamic, RotationMinutes = 5 },
            Guid.NewGuid(),
            orgId);

        var current = await ctx.Get.ExecuteAsync(locationId, orgId);
        Assert.False(current.VerificationEnabled);
        Assert.Null(current.Code);

        var validation = await ctx.Lookup.ValidateQrCodeAsync(issued.Code!, orgId);
        Assert.False(validation.IsValid);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ctx.Generate.ExecuteAsync(locationId, Guid.NewGuid(), orgId));
    }

    [Fact]
    public async Task SwitchingFromStaticToDynamic_InvalidatesStaticCode()
    {
        using var db = GetInMemoryDbContext();
        var ctx = new QrTestContext(db, new MutableTimeProvider());
        var (orgId, locationId) = await SeedWorkLocationAsync(db, mode: QrCodeMode.Static);

        var staticCode = await ctx.Generate.ExecuteAsync(locationId, Guid.NewGuid(), orgId);

        var settings = await ctx.UpdateSettings.ExecuteAsync(
            locationId,
            new UpdateQrCodeSettingsRequest { VerificationEnabled = true, Mode = QrCodeMode.Dynamic, RotationMinutes = 10 },
            Guid.NewGuid(),
            orgId);

        Assert.Equal("Dynamic", settings.Mode);
        Assert.NotEqual(staticCode.Code, settings.CurrentCode);

        var staticValidation = await ctx.Lookup.ValidateQrCodeAsync(staticCode.Code!, orgId);
        Assert.False(staticValidation.IsValid);

        var dynamicValidation = await ctx.Lookup.ValidateQrCodeAsync(settings.CurrentCode!, orgId);
        Assert.True(dynamicValidation.IsValid);
    }

    [Fact]
    public async Task DynamicToken_BelongsToIssuingWorkLocationOnly()
    {
        using var db = GetInMemoryDbContext();
        var ctx = new QrTestContext(db, new MutableTimeProvider());
        var (orgId, locationId) = await SeedWorkLocationAsync(db);
        var (otherOrgId, otherLocationId) = await SeedWorkLocationAsync(db);

        var code = await ctx.Get.ExecuteAsync(locationId, orgId);

        var sameLocation = await ctx.Lookup.ValidateQrCodeAsync(code.Code!, orgId);
        Assert.True(sameLocation.IsValid);
        Assert.Equal(locationId, sameLocation.WorkLocationId);

        var otherOrg = await ctx.Lookup.ValidateQrCodeAsync(code.Code!, otherOrgId);
        Assert.False(otherOrg.IsValid);
        Assert.Contains("سازمان دیگری", otherOrg.ErrorMessage);

        var otherLocationSettings = await ctx.GetSettings.ExecuteAsync(otherLocationId, otherOrgId);
        Assert.NotEqual(code.Code, otherLocationSettings.CurrentCode);
    }

    [Fact]
    public async Task Validation_RejectsInvalidTamperedAndRandomTokens()
    {
        using var db = GetInMemoryDbContext();
        var ctx = new QrTestContext(db, new MutableTimeProvider());
        var (orgId, locationId) = await SeedWorkLocationAsync(db);

        var code = await ctx.Get.ExecuteAsync(locationId, orgId);
        Assert.NotNull(code.Code);

        string[] parts = code.Code!.Split('.');
        string tamperedWithDifferentWindow = string.Join('.', parts[0], parts[1], "0", parts[3]);

        Assert.False((await ctx.Lookup.ValidateQrCodeAsync(string.Empty, orgId)).IsValid);
        Assert.False((await ctx.Lookup.ValidateQrCodeAsync("not-a-qr-token", orgId)).IsValid);
        Assert.False((await ctx.Lookup.ValidateQrCodeAsync(tamperedWithDifferentWindow, orgId)).IsValid);
        Assert.False((await ctx.Lookup.ValidateQrCodeAsync($"{parts[0]}.{parts[1]}.{parts[2]}.AAAA", orgId)).IsValid);
    }

    [Fact]
    public async Task QrContent_DoesNotExposeInternalIdentifiers()
    {
        using var db = GetInMemoryDbContext();
        var ctx = new QrTestContext(db, new MutableTimeProvider());
        var (orgId, locationId) = await SeedWorkLocationAsync(db);

        var code = await ctx.Get.ExecuteAsync(locationId, orgId);

        Assert.NotNull(code.Code);
        Assert.DoesNotContain(locationId.ToString(), code.Code, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(orgId.ToString(), code.Code, StringComparison.OrdinalIgnoreCase);

        var location = await db.WorkLocations.AsNoTracking().FirstAsync(w => w.Id == locationId);
        Assert.NotNull(location.QrCodeReference);
        Assert.NotNull(location.QrCodeSecret);
        Assert.DoesNotContain(location.QrCodeSecret!, code.Code!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateSettings_CrossOrganization_ThrowsUnauthorizedAccessException()
    {
        using var db = GetInMemoryDbContext();
        var ctx = new QrTestContext(db, new MutableTimeProvider());
        var (_, locationId) = await SeedWorkLocationAsync(db);
        var otherOrgId = Guid.NewGuid();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => ctx.UpdateSettings.ExecuteAsync(
            locationId,
            new UpdateQrCodeSettingsRequest { VerificationEnabled = true, Mode = QrCodeMode.Dynamic, RotationMinutes = 5 },
            Guid.NewGuid(),
            otherOrgId));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            ctx.GetSettings.ExecuteAsync(locationId, otherOrgId));
    }

    [Fact]
    public async Task GetSettings_ExposesPredefinedIntervalsAndCurrentValidity()
    {
        using var db = GetInMemoryDbContext();
        var ctx = new QrTestContext(db, new MutableTimeProvider());
        var (orgId, locationId) = await SeedWorkLocationAsync(db);

        var settings = await ctx.GetSettings.ExecuteAsync(locationId, orgId);

        Assert.Equal(new[] { 1, 5, 10, 15, 30, 60 }, settings.PredefinedRotationMinutes);
        Assert.Equal(QrCodeOptions.MinRotationMinutes, settings.MinRotationMinutes);
        Assert.Equal(QrCodeOptions.MaxRotationMinutes, settings.MaxRotationMinutes);
        Assert.True(settings.VerificationEnabled);
        Assert.NotNull(settings.CurrentCode);
        Assert.NotNull(settings.ExpiresAtUtc);
        Assert.NotNull(settings.RemainingValiditySeconds);
    }

    [Fact]
    public async Task Controller_GenerateQrCode_AsCenterManager_ForOwnOrganization_Succeeds()
    {
        using var db = GetInMemoryDbContext();
        var ctx = new QrTestContext(db, new MutableTimeProvider());
        var (orgId, locationId) = await SeedWorkLocationAsync(db);
        var controller = ctx.CreateController(Guid.NewGuid(), "CenterManager", orgId);

        var actionResult = await controller.GenerateQrCode(locationId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var dto = Assert.IsType<QrCodeResponse>(okResult.Value);
        Assert.False(string.IsNullOrWhiteSpace(dto.Code));
        Assert.NotNull(dto.ExpiresAtUtc);
    }

    [Fact]
    public async Task Controller_GenerateQrCode_AsCenterManager_CrossOrganizationAttempt_ReturnsForbid()
    {
        using var db = GetInMemoryDbContext();
        var ctx = new QrTestContext(db, new MutableTimeProvider());
        var (_, locationId) = await SeedWorkLocationAsync(db);
        var controller = ctx.CreateController(Guid.NewGuid(), "CenterManager", Guid.NewGuid());

        var actionResult = await controller.GenerateQrCode(locationId, CancellationToken.None);

        Assert.IsType<ForbidResult>(actionResult);
    }

    [Fact]
    public async Task Controller_UpdateQrCodeSettings_AsCenterManager_ForOwnOrganization_Succeeds()
    {
        using var db = GetInMemoryDbContext();
        var ctx = new QrTestContext(db, new MutableTimeProvider());
        var (orgId, locationId) = await SeedWorkLocationAsync(db);
        var controller = ctx.CreateController(Guid.NewGuid(), "CenterManager", orgId);

        var actionResult = await controller.UpdateQrCodeSettings(
            locationId,
            new UpdateQrCodeSettingsRequest { VerificationEnabled = true, Mode = QrCodeMode.Static, RotationMinutes = 30 },
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var dto = Assert.IsType<QrCodeSettingsResponse>(okResult.Value);
        Assert.Equal("Static", dto.Mode);
        Assert.False(string.IsNullOrWhiteSpace(dto.CurrentCode));
    }

    [Fact]
    public async Task Controller_UpdateQrCodeSettings_CrossOrganizationAttempt_ReturnsForbid()
    {
        using var db = GetInMemoryDbContext();
        var ctx = new QrTestContext(db, new MutableTimeProvider());
        var (_, locationId) = await SeedWorkLocationAsync(db);
        var controller = ctx.CreateController(Guid.NewGuid(), "CenterManager", Guid.NewGuid());

        var actionResult = await controller.UpdateQrCodeSettings(
            locationId,
            new UpdateQrCodeSettingsRequest { VerificationEnabled = true, Mode = QrCodeMode.Dynamic, RotationMinutes = 5 },
            CancellationToken.None);

        Assert.IsType<ForbidResult>(actionResult);
    }

    [Fact]
    public async Task Controller_GetQrCode_AsSystemAdmin_ReturnsCurrentCode()
    {
        using var db = GetInMemoryDbContext();
        var ctx = new QrTestContext(db, new MutableTimeProvider());
        var (_, locationId) = await SeedWorkLocationAsync(db);
        var controller = ctx.CreateController(Guid.NewGuid(), "SystemAdmin", null);

        var actionResult = await controller.GetQrCode(locationId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var dto = Assert.IsType<QrCodeResponse>(okResult.Value);
        Assert.False(string.IsNullOrWhiteSpace(dto.Code));
    }

    [Fact]
    public async Task Controller_GenerateQrCodeMethod_HasCorrectAttributes()
    {
        var methodInfo = typeof(WorkLocationsController).GetMethod("GenerateQrCode");

        Assert.NotNull(methodInfo);
        var httpPostAttr = methodInfo.GetCustomAttribute<HttpPostAttribute>();
        Assert.NotNull(httpPostAttr);
        Assert.Equal("{workLocationId}/qr", httpPostAttr.Template);
    }

    private static async Task<bool> TryValidateAsync(QrTestContext ctx, string code, Guid organizationId)
    {
        var result = await ctx.Lookup.ValidateQrCodeAsync(code, organizationId);
        return result.IsValid;
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using AriaHR.Modules.Identity.API.Controllers;
using AriaHR.Modules.Identity.Application.DTOs;
using AriaHR.Modules.Identity.Application.Options;
using AriaHR.Modules.Identity.Application.UseCases;
using AriaHR.Modules.Identity.Domain.Entities;
using AriaHR.Modules.Identity.Infrastructure.Authentication;
using AriaHR.Modules.Identity.Infrastructure.Persistence;
using AriaHR.Modules.Identity.Infrastructure.Repositories;
using AriaHR.Modules.Identity.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AriaHR.Modules.Identity.Tests;

public class TestHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Development;
    public string ApplicationName { get; set; } = "AriaHR.Tests";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
}

public class AuthControllerTests
{
    private readonly DbContextOptions<IdentityDbContext> _dbContextOptions;
    private readonly IOptions<JwtOptions> _jwtOptions;
    private readonly IOptions<OtpOptions> _otpOptions;

    public AuthControllerTests()
    {
        _dbContextOptions = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _jwtOptions = Options.Create(new JwtOptions
        {
            Issuer = "AriaHR.TestIssuer",
            Audience = "AriaHR.TestAudience",
            SecretKey = "SUPER_SECRET_KEY_FOR_UNIT_TESTING_PURPOSES_ONLY_MIN_256_BITS",
            AccessTokenExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        });

        _otpOptions = Options.Create(new OtpOptions
        {
            CodeLength = 4,
            ExpirationMinutes = 2,
            MaxAttempts = 5
        });
    }

    private IdentityDbContext CreateDbContext() => new(_dbContextOptions);

    private AuthController CreateController(IdentityDbContext dbContext, string environmentName = "Development")
    {
        var userRepo = new UserRepository(dbContext);
        var roleRepo = new RoleRepository(dbContext);
        var userRoleRepo = new UserRoleRepository(dbContext);
        var otpRepo = new OtpCodeRepository(dbContext);
        var tokenService = new JwtTokenService(_jwtOptions);
        var notificationService = new AuthNotificationService(NullLogger<AuthNotificationService>.Instance);

        var sendOtpUseCase = new SendOtpUseCase(userRepo, otpRepo, notificationService, tokenService, _otpOptions);
        var verifyOtpUseCase = new VerifyOtpUseCase(userRepo, otpRepo, userRoleRepo, tokenService, _otpOptions);
        var getCurrentUserUseCase = new GetCurrentUserUseCase(userRepo, userRoleRepo);

        var env = new TestHostEnvironment { EnvironmentName = environmentName };

        var controller = new AuthController(sendOtpUseCase, verifyOtpUseCase, getCurrentUserUseCase, env)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        return controller;
    }

    [Fact]
    public async Task SendOtp_DevelopmentEnvironment_Returns200OK_WithOtpCode()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "مولود",
            LastName = "ناصرالمعمارین",
            PhoneNumber = "09376421351",
            Email = "admin1@ariahr.com",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext, "Development");
        var request = new SendOtpRequest("09376421351");

        // Act
        var result = await controller.SendOtp(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        dynamic responseValue = okResult.Value!;
        var responseDict = ((object)okResult.Value!).GetType().GetProperties()
            .ToDictionary(p => p.Name, p => p.GetValue(okResult.Value));

        Assert.True(responseDict.ContainsKey("otpCode"));
        string returnedOtp = (string)responseDict["otpCode"]!;
        Assert.Equal(4, returnedOtp.Length);

        var otpCode = await dbContext.OtpCodes.FirstOrDefaultAsync(o => o.UserId == user.Id);
        Assert.NotNull(otpCode);
        Assert.False(otpCode.IsUsed);
        Assert.Equal("09376421351", otpCode.PhoneNumber);
        // Verify database holds hashed version, not raw version
        Assert.NotEqual(returnedOtp, otpCode.CodeHash);
    }

    [Fact]
    public async Task SendOtp_ProductionEnvironment_Returns200OK_WithoutOtpCode()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "مولود",
            LastName = "ناصرالمعمارین",
            PhoneNumber = "09376421351",
            Email = "admin1@ariahr.com",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext, "Production");
        var request = new SendOtpRequest("09376421351");

        // Act
        var result = await controller.SendOtp(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var responseDict = ((object)okResult.Value!).GetType().GetProperties()
            .ToDictionary(p => p.Name, p => p.GetValue(okResult.Value));

        Assert.False(responseDict.ContainsKey("otpCode"));
        Assert.Equal("کد تایید با موفقیت ارسال شد", responseDict["message"]);
    }

    [Fact]
    public async Task SendOtp_ProductionEnvironment_DoesNotExposeOtpCodeInResponse()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "مولود",
            LastName = "ناصرالمعمارین",
            PhoneNumber = "09376421351",
            Email = "admin1@ariahr.com",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();

        var prodEnv = new TestHostEnvironment { EnvironmentName = Environments.Production };
        var controller = CreateController(dbContext, prodEnv);
        var request = new SendOtpRequest("09376421351");

        // Act
        var result = await controller.SendOtp(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var jsonValue = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        Assert.DoesNotContain("otpCode", jsonValue);
    }

    [Fact]
    public async Task SendOtp_UnknownUser_Returns400BadRequest()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext);
        var request = new SendOtpRequest("09120000000");

        // Act
        var result = await controller.SendOtp(request, CancellationToken.None);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
    }

    [Fact]
    public async Task VerifyOtp_ValidOtp_Returns200OK_WithTokenAndUser()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "مرتضی",
            LastName = "سلطانی",
            PhoneNumber = "09183159274",
            Email = "admin2@ariahr.com",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        var role = new Role { Id = Guid.NewGuid(), Name = "SystemAdmin", Description = "Admin" };
        var userRole = new UserRole { UserId = user.Id, RoleId = role.Id };

        await dbContext.Users.AddAsync(user);
        await dbContext.Roles.AddAsync(role);
        await dbContext.UserRoles.AddAsync(userRole);

        var tokenService = new JwtTokenService(_jwtOptions);
        string rawCode = "1234";
        var otp = new OtpCode
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PhoneNumber = "09183159274",
            CodeHash = tokenService.HashToken(rawCode),
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(2),
            IsUsed = false,
            AttemptCount = 0
        };
        await dbContext.OtpCodes.AddAsync(otp);
        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext);
        var request = new VerifyOtpRequest("09183159274", rawCode);

        // Act
        var result = await controller.VerifyOtp(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<VerifyOtpResponse>(okResult.Value);

        Assert.NotNull(response.AccessToken);
        Assert.Equal("مرتضی", response.User.FirstName);
        Assert.Equal("سلطانی", response.User.LastName);
        Assert.Contains("SystemAdmin", response.User.Roles);

        var updatedOtp = await dbContext.OtpCodes.FirstAsync(o => o.Id == otp.Id);
        Assert.True(updatedOtp.IsUsed);
    }

    [Fact]
    public async Task GetCurrentUser_Authenticated_Returns200OK_WithUserData()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "مولود",
            LastName = "ناصرالمعمارین",
            PhoneNumber = "09376421351",
            Email = "admin3@ariahr.com",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        var role = new Role { Id = Guid.NewGuid(), Name = "SystemAdmin" };
        var userRole = new UserRole { UserId = user.Id, RoleId = role.Id };

        await dbContext.Users.AddAsync(user);
        await dbContext.Roles.AddAsync(role);
        await dbContext.UserRoles.AddAsync(userRole);
        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, "مولود ناصرالمعمارین")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var actionResult = await controller.GetCurrentUser(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var response = Assert.IsType<UserResponse>(okResult.Value);

        Assert.Equal(user.Id, response.Id);
        Assert.Equal("مولود", response.FirstName);
        Assert.Equal("ناصرالمعمارین", response.LastName);
        Assert.Contains("SystemAdmin", response.Roles);
    }

    [Fact]
    public void SendOtp_HasAllowAnonymousAttribute()
    {
        var methodInfo = typeof(AuthController).GetMethod(nameof(AuthController.SendOtp));
        var allowAnonymousAttr = methodInfo?.GetCustomAttribute<AllowAnonymousAttribute>();
        Assert.NotNull(allowAnonymousAttr);
    }

    [Fact]
    public void VerifyOtp_HasAllowAnonymousAttribute()
    {
        var methodInfo = typeof(AuthController).GetMethod(nameof(AuthController.VerifyOtp));
        var allowAnonymousAttr = methodInfo?.GetCustomAttribute<AllowAnonymousAttribute>();
        Assert.NotNull(allowAnonymousAttr);
    }

    [Fact]
    public void GetCurrentUser_HasAuthorizeAttribute()
    {
        var methodInfo = typeof(AuthController).GetMethod(nameof(AuthController.GetCurrentUser));
        var authorizeAttr = methodInfo?.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorizeAttr);
    }
}

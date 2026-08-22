using System.Reflection;
using AriaHR.Modules.Identity.API.Controllers;
using AriaHR.Modules.Identity.Application.DTOs;
using AriaHR.Modules.Identity.Application.UseCases.ForgotPassword;
using AriaHR.Modules.Identity.Application.UseCases.Login;
using AriaHR.Modules.Identity.Application.UseCases.RefreshToken;
using AriaHR.Modules.Identity.Domain.Entities;
using AriaHR.Modules.Identity.Infrastructure.Authentication;
using AriaHR.Modules.Identity.Infrastructure.Persistence;
using AriaHR.Modules.Identity.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace AriaHR.Modules.Identity.Tests;

public class AuthControllerTests
{
    private readonly DbContextOptions<IdentityDbContext> _dbContextOptions;
    private readonly IOptions<JwtOptions> _jwtOptions;

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
    }

    private IdentityDbContext CreateDbContext() => new(_dbContextOptions);

    private AuthController CreateController(IdentityDbContext dbContext)
    {
        var userRepo = new UserRepository(dbContext);
        var userRoleRepo = new UserRoleRepository(dbContext);
        var refreshRepo = new RefreshTokenRepository(dbContext);
        var resetRepo = new PasswordResetRepository(dbContext);
        var passwordService = new PasswordService();
        var tokenService = new JwtTokenService(_jwtOptions);

        var loginUseCase = new LoginUseCase(userRepo, userRoleRepo, refreshRepo, passwordService, tokenService);
        var refreshUseCase = new RefreshTokenUseCase(refreshRepo, userRepo, userRoleRepo, tokenService);
        var forgotUseCase = new ForgotPasswordUseCase(userRepo, resetRepo, null!, tokenService);
        var resetUseCase = new ResetPasswordUseCase(userRepo, resetRepo, refreshRepo, passwordService, tokenService);

        var controller = new AuthController(loginUseCase, refreshUseCase, forgotUseCase, resetUseCase)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        return controller;
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200OK_WithTokenDetails()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var passwordService = new PasswordService();

        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "1234567890",
            Email = "user@ariahr.com",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        testUser.PasswordHash = passwordService.HashPassword(testUser, "ValidPass123!");
        await dbContext.Users.AddAsync(testUser);
        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext);
        var request = new LoginRequest("1234567890", "ValidPass123!");

        // Act
        var actionResult = await controller.Login(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var response = Assert.IsType<AuthenticationResponse>(okResult.Value);

        Assert.NotNull(response.AccessToken);
        Assert.NotNull(response.RefreshToken);
        Assert.Equal("Bearer", response.TokenType);
        Assert.True(response.ExpiresIn > 0);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns401Unauthorized()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var passwordService = new PasswordService();

        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "1234567890",
            Email = "user@ariahr.com",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        testUser.PasswordHash = passwordService.HashPassword(testUser, "ValidPass123!");
        await dbContext.Users.AddAsync(testUser);
        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext);
        var request = new LoginRequest("1234567890", "WrongPassword!");

        // Act
        var actionResult = await controller.Login(request, CancellationToken.None);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(actionResult);
        var problem = Assert.IsType<ProblemDetails>(unauthorizedResult.Value);
        Assert.Equal(StatusCodes.Status401Unauthorized, problem.Status);
        Assert.Equal("Authentication failed", problem.Title);
    }

    [Fact]
    public async Task Login_WithMissingNationalCode_Returns400BadRequest()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext);
        var request = new LoginRequest("", "Password123!");

        // Act
        var actionResult = await controller.Login(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        var problem = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
    }

    [Fact]
    public async Task Login_WithMissingPassword_Returns400BadRequest()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext);
        var request = new LoginRequest("1234567890", "");

        // Act
        var actionResult = await controller.Login(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        var problem = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
    }

    [Fact]
    public async Task Login_WithInvalidNationalCodeFormat_Returns400BadRequest()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext);
        var request = new LoginRequest("1234", "Password123!");

        // Act
        var actionResult = await controller.Login(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        var problem = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
    }

    [Fact]
    public void Login_HasAllowAnonymousAttribute()
    {
        // Arrange & Act
        var methodInfo = typeof(AuthController).GetMethod(nameof(AuthController.Login));
        var allowAnonymousAttr = methodInfo?.GetCustomAttribute<AllowAnonymousAttribute>();

        // Assert
        Assert.NotNull(allowAnonymousAttr);
    }

    [Fact]
    public void ProtectedEndpoints_HaveAuthorizeAttribute()
    {
        // Arrange & Act
        var rolesControllerAttr = typeof(RolesController).GetCustomAttribute<AuthorizeAttribute>();

        // Assert
        Assert.NotNull(rolesControllerAttr);
    }

    [Fact]
    public void LoginResponse_DoesNotExposeSensitiveFields()
    {
        // Arrange
        var properties = typeof(AuthenticationResponse).GetProperties();

        // Assert
        Assert.DoesNotContain(properties, p => p.Name.Contains("Password", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, p => p.Name.Contains("SecurityStamp", StringComparison.OrdinalIgnoreCase));
    }
}

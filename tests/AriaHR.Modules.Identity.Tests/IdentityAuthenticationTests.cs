using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AriaHR.Modules.Identity.Application.DTOs;
using AriaHR.Modules.Identity.Application.UseCases.ForgotPassword;
using AriaHR.Modules.Identity.Application.UseCases.Login;
using AriaHR.Modules.Identity.Application.UseCases.RefreshToken;
using AriaHR.Modules.Identity.Application.UseCases.Role;
using AriaHR.Modules.Identity.Domain.Entities;
using AriaHR.Modules.Identity.Infrastructure.Authentication;
using AriaHR.Modules.Identity.Infrastructure.Persistence;
using AriaHR.Modules.Identity.Infrastructure.Repositories;
using AriaHR.Modules.Identity.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AriaHR.Modules.Identity.Tests;

public class IdentityAuthenticationTests
{
    private readonly DbContextOptions<IdentityDbContext> _dbContextOptions;
    private readonly IOptions<JwtOptions> _jwtOptions;

    public IdentityAuthenticationTests()
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

    [Fact]
    public async Task ValidLogin_Returns_AccessToken_And_RefreshToken()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);
        var userRoleRepo = new UserRoleRepository(dbContext);
        var refreshRepo = new RefreshTokenRepository(dbContext);
        var passwordService = new PasswordService();
        var tokenService = new JwtTokenService(_jwtOptions);

        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "1234567890", // NationalCode
            Email = "test@ariahr.com",
            PhoneNumber = "09123456789",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        testUser.PasswordHash = passwordService.HashPassword(testUser, "Password123!");
        await dbContext.Users.AddAsync(testUser);
        await dbContext.SaveChangesAsync();

        var loginUseCase = new LoginUseCase(userRepo, userRoleRepo, refreshRepo, passwordService, tokenService);

        // Act
        var response = await loginUseCase.ExecuteAsync(new LoginRequest("1234567890", "Password123!"));

        // Assert
        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));

        // Read JWT token
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(response.AccessToken);
        Assert.Equal("AriaHR.TestIssuer", jwtToken.Issuer);
        Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == testUser.Id.ToString());
        Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Name && c.Value == "1234567890");
    }

    [Fact]
    public async Task InvalidPassword_Login_Returns_Null()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);
        var userRoleRepo = new UserRoleRepository(dbContext);
        var refreshRepo = new RefreshTokenRepository(dbContext);
        var passwordService = new PasswordService();
        var tokenService = new JwtTokenService(_jwtOptions);

        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "1234567890",
            Email = "test@ariahr.com",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        testUser.PasswordHash = passwordService.HashPassword(testUser, "CorrectPassword123!");
        await dbContext.Users.AddAsync(testUser);
        await dbContext.SaveChangesAsync();

        var loginUseCase = new LoginUseCase(userRepo, userRoleRepo, refreshRepo, passwordService, tokenService);

        // Act
        var response = await loginUseCase.ExecuteAsync(new LoginRequest("1234567890", "WrongPassword!"));

        // Assert
        Assert.Null(response);
    }

    [Fact]
    public async Task RefreshTokenRotation_Rotates_Token_And_Invalidates_Old_Token()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);
        var userRoleRepo = new UserRoleRepository(dbContext);
        var refreshRepo = new RefreshTokenRepository(dbContext);
        var passwordService = new PasswordService();
        var tokenService = new JwtTokenService(_jwtOptions);

        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "1234567890",
            Email = "test@ariahr.com",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        await dbContext.Users.AddAsync(testUser);
        await dbContext.SaveChangesAsync();

        var loginUseCase = new LoginUseCase(userRepo, userRoleRepo, refreshRepo, passwordService, tokenService);
        var refreshUseCase = new RefreshTokenUseCase(refreshRepo, userRepo, userRoleRepo, tokenService);

        var initialAuth = await loginUseCase.ExecuteAsync(new LoginRequest("1234567890", ""));
        // Manually create initial token if login failed due to password
        testUser.PasswordHash = passwordService.HashPassword(testUser, "Pass123!");
        await userRepo.UpdateAsync(testUser);

        initialAuth = await loginUseCase.ExecuteAsync(new LoginRequest("1234567890", "Pass123!"));
        Assert.NotNull(initialAuth);

        // Act - Perform Refresh
        var rotatedAuth = await refreshUseCase.ExecuteAsync(new RefreshTokenRequest(initialAuth.RefreshToken));

        // Assert
        Assert.NotNull(rotatedAuth);
        Assert.NotEqual(initialAuth.RefreshToken, rotatedAuth.RefreshToken);

        // Attempting to reuse old refresh token MUST fail
        var reusedAttempt = await refreshUseCase.ExecuteAsync(new RefreshTokenRequest(initialAuth.RefreshToken));
        Assert.Null(reusedAttempt);
    }

    [Fact]
    public async Task ForgotPassword_And_ResetPassword_Flow_Succeeds()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);
        var userRoleRepo = new UserRoleRepository(dbContext);
        var refreshRepo = new RefreshTokenRepository(dbContext);
        var resetRepo = new PasswordResetRepository(dbContext);
        var passwordService = new PasswordService();
        var tokenService = new JwtTokenService(_jwtOptions);
        var notificationService = new AuthNotificationService(NullLogger<AuthNotificationService>.Instance);

        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "1234567890",
            Email = "user@ariahr.com",
            PhoneNumber = "09123456789",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        testUser.PasswordHash = passwordService.HashPassword(testUser, "OldPassword123!");
        await dbContext.Users.AddAsync(testUser);
        await dbContext.SaveChangesAsync();

        var forgotUseCase = new ForgotPasswordUseCase(userRepo, resetRepo, notificationService, tokenService);
        var resetUseCase = new ResetPasswordUseCase(userRepo, resetRepo, refreshRepo, passwordService, tokenService);

        // Act 1: Forgot Password
        await forgotUseCase.ExecuteAsync(new ForgotPasswordRequest("09123456789"));

        var challenge = await dbContext.PasswordResetChallenges
            .FirstOrDefaultAsync(c => c.UserId == testUser.Id);
        Assert.NotNull(challenge);

        // Code verification hack for test assertions: challenge stores code hash
        // We simulate submitting a valid code challenge by inserting code
        var customCode = "123456";
        challenge.CodeHash = tokenService.HashToken(customCode);
        await dbContext.SaveChangesAsync();

        // Act 2: Reset Password
        bool resetResult = await resetUseCase.ExecuteAsync(new ResetPasswordRequest("09123456789", customCode, "NewSecret123!"));

        // Assert
        Assert.True(resetResult);

        var updatedUser = await userRepo.GetByIdAsync(testUser.Id);
        Assert.NotNull(updatedUser);
        Assert.True(passwordService.VerifyPassword(updatedUser, updatedUser.PasswordHash, "NewSecret123!"));
    }

    [Fact]
    public async Task Admin_Can_Create_And_Assign_Role()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);
        var roleRepo = new RoleRepository(dbContext);
        var userRoleRepo = new UserRoleRepository(dbContext);

        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "9998887776",
            Email = "admin@ariahr.com",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        await dbContext.Users.AddAsync(testUser);
        await dbContext.SaveChangesAsync();

        var roleUseCase = new RoleUseCase(roleRepo, userRepo, userRoleRepo);

        // Act 1: Create Role
        var createRoleResult = await roleUseCase.CreateRoleAsync(new CreateRoleRequest("Admin", "Administrator role"));

        // Assert 1
        Assert.NotNull(createRoleResult);
        Assert.Equal("Admin", createRoleResult.Name);

        // Act 2: Assign Role
        bool assignResult = await roleUseCase.AssignRoleAsync(new AssignRoleRequest(testUser.Id, "Admin"));

        // Assert 2
        Assert.True(assignResult);

        var userRoles = await userRoleRepo.GetRolesByUserIdAsync(testUser.Id);
        Assert.Contains(userRoles, r => r.Name == "Admin");
    }
}

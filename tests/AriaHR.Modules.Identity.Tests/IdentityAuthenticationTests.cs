using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AriaHR.Modules.Identity.Application.DTOs;
using AriaHR.Modules.Identity.Application.Options;
using AriaHR.Modules.Identity.Application.UseCases;
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
    private readonly IOptions<OtpOptions> _otpOptions;

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

        _otpOptions = Options.Create(new OtpOptions
        {
            CodeLength = 4,
            ExpirationMinutes = 2,
            MaxAttempts = 5
        });
    }

    private IdentityDbContext CreateDbContext() => new(_dbContextOptions);

    [Fact]
    public async Task SendOtp_ExistingUser_CreatesHashedOtpCode()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);
        var otpRepo = new OtpCodeRepository(dbContext);
        var tokenService = new JwtTokenService(_jwtOptions);
        var notificationService = new AuthNotificationService(NullLogger<AuthNotificationService>.Instance);

        var testUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Ali",
            LastName = "Ahmadi",
            PhoneNumber = "09121112233",
            Email = "ali@ariahr.com",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        await dbContext.Users.AddAsync(testUser);
        await dbContext.SaveChangesAsync();

        var sendOtpUseCase = new SendOtpUseCase(userRepo, otpRepo, notificationService, tokenService, _otpOptions);

        // Act
        var result = await sendOtpUseCase.ExecuteAsync(new SendOtpRequest("09121112233"));

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.OtpCode);
        Assert.Equal(4, result.OtpCode.Length);

        var createdOtp = await dbContext.OtpCodes.FirstOrDefaultAsync(o => o.UserId == testUser.Id);
        Assert.NotNull(createdOtp);
        Assert.Equal("09121112233", createdOtp.PhoneNumber);
        Assert.False(createdOtp.IsUsed);
        Assert.True(createdOtp.ExpiresAtUtc > DateTime.UtcNow);

        // Raw code must not match database stored hash directly
        Assert.NotEqual(result.OtpCode, createdOtp.CodeHash);
        // Hashing raw code must match database stored hash
        Assert.Equal(tokenService.HashToken(result.OtpCode), createdOtp.CodeHash);
    }

    [Fact]
    public async Task SendOtp_ThenVerifyOtp_WithReturnedCode_Succeeds_AndPreventsReuse()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);
        var otpRepo = new OtpCodeRepository(dbContext);
        var userRoleRepo = new UserRoleRepository(dbContext);
        var tokenService = new JwtTokenService(_jwtOptions);
        var notificationService = new AuthNotificationService(NullLogger<AuthNotificationService>.Instance);

        var testUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "SystemAdmin",
            LastName = "User",
            PhoneNumber = "09376421351",
            Email = "admin@ariahr.com",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        var role = new Role { Id = Guid.NewGuid(), Name = "SystemAdmin", Description = "System Admin Role" };
        var userRole = new UserRole { UserId = testUser.Id, RoleId = role.Id };

        await dbContext.Users.AddAsync(testUser);
        await dbContext.Roles.AddAsync(role);
        await dbContext.UserRoles.AddAsync(userRole);
        await dbContext.SaveChangesAsync();

        var sendOtpUseCase = new SendOtpUseCase(userRepo, otpRepo, notificationService, tokenService, _otpOptions);
        var verifyOtpUseCase = new VerifyOtpUseCase(userRepo, otpRepo, userRoleRepo, tokenService, _otpOptions);

        // 1. Send OTP
        var sendResult = await sendOtpUseCase.ExecuteAsync(new SendOtpRequest("09376421351"));
        Assert.True(sendResult.Success);
        Assert.NotNull(sendResult.OtpCode);

        // 2. Verify with exact generated code
        var verifyResult = await verifyOtpUseCase.ExecuteAsync(new VerifyOtpRequest("09376421351", sendResult.OtpCode));
        Assert.True(verifyResult.Success);
        Assert.NotNull(verifyResult.Response);
        Assert.Contains("SystemAdmin", verifyResult.Response.User.Roles);

        // 3. Reusing same code must fail
        var reuseResult = await verifyOtpUseCase.ExecuteAsync(new VerifyOtpRequest("09376421351", sendResult.OtpCode));
        Assert.False(reuseResult.Success);
        Assert.Equal("INVALID_CODE", reuseResult.ErrorType);
    }

    [Fact]
    public async Task SendOtp_UnknownUser_Fails_AndDoesNotCreateAccount()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);
        var otpRepo = new OtpCodeRepository(dbContext);
        var tokenService = new JwtTokenService(_jwtOptions);
        var notificationService = new AuthNotificationService(NullLogger<AuthNotificationService>.Instance);

        var sendOtpUseCase = new SendOtpUseCase(userRepo, otpRepo, notificationService, tokenService, _otpOptions);

        // Act
        var result = await sendOtpUseCase.ExecuteAsync(new SendOtpRequest("09999999999"));

        // Assert
        Assert.False(result.Success);
        Assert.Contains("یافت نشد", result.ErrorMessage);

        var userCount = await dbContext.Users.CountAsync();
        Assert.Equal(0, userCount);
    }

    [Fact]
    public async Task VerifyOtp_CorrectOtp_IssuesJwtWithRoleClaims()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);
        var otpRepo = new OtpCodeRepository(dbContext);
        var userRoleRepo = new UserRoleRepository(dbContext);
        var tokenService = new JwtTokenService(_jwtOptions);

        var testUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Sara",
            LastName = "Rezai",
            PhoneNumber = "09123334455",
            Email = "sara@ariahr.com",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        var role = new Role { Id = Guid.NewGuid(), Name = "SystemAdmin", Description = "Admin" };
        var userRole = new UserRole { UserId = testUser.Id, RoleId = role.Id };

        await dbContext.Users.AddAsync(testUser);
        await dbContext.Roles.AddAsync(role);
        await dbContext.UserRoles.AddAsync(userRole);

        string code = "4321";
        var otpCode = new OtpCode
        {
            Id = Guid.NewGuid(),
            UserId = testUser.Id,
            PhoneNumber = "09123334455",
            CodeHash = tokenService.HashToken(code),
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(2),
            IsUsed = false,
            AttemptCount = 0
        };
        await dbContext.OtpCodes.AddAsync(otpCode);
        await dbContext.SaveChangesAsync();

        var verifyOtpUseCase = new VerifyOtpUseCase(userRepo, otpRepo, userRoleRepo, tokenService, _otpOptions);

        // Act
        var result = await verifyOtpUseCase.ExecuteAsync(new VerifyOtpRequest("09123334455", code));

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Response);
        Assert.False(string.IsNullOrWhiteSpace(result.Response.AccessToken));
        Assert.Contains("SystemAdmin", result.Response.User.Roles);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(result.Response.AccessToken);
        Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Role && c.Value == "SystemAdmin");
        Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == testUser.Id.ToString());
    }

    [Fact]
    public async Task VerifyOtp_UserWithOrganization_IncludesOrganizationIdAndJwtClaim()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);
        var otpRepo = new OtpCodeRepository(dbContext);
        var userRoleRepo = new UserRoleRepository(dbContext);
        var tokenService = new JwtTokenService(_jwtOptions);

        var orgId = Guid.NewGuid();
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Center",
            LastName = "Manager",
            PhoneNumber = "09000000002",
            Email = "manager@ariahr.com",
            IsActive = true,
            OrganizationId = orgId,
            CreatedAtUtc = DateTime.UtcNow
        };
        var role = new Role { Id = Guid.NewGuid(), Name = "CenterManager", Description = "Center Manager" };
        var userRole = new UserRole { UserId = testUser.Id, RoleId = role.Id };

        await dbContext.Users.AddAsync(testUser);
        await dbContext.Roles.AddAsync(role);
        await dbContext.UserRoles.AddAsync(userRole);

        string code = "1234";
        var otpCode = new OtpCode
        {
            Id = Guid.NewGuid(),
            UserId = testUser.Id,
            PhoneNumber = "09000000002",
            CodeHash = tokenService.HashToken(code),
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(2),
            IsUsed = false,
            AttemptCount = 0
        };
        await dbContext.OtpCodes.AddAsync(otpCode);
        await dbContext.SaveChangesAsync();

        var verifyOtpUseCase = new VerifyOtpUseCase(userRepo, otpRepo, userRoleRepo, tokenService, _otpOptions);

        // Act
        var result = await verifyOtpUseCase.ExecuteAsync(new VerifyOtpRequest("09000000002", code));

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Response);
        Assert.Equal(orgId, result.Response.User.OrganizationId);
        Assert.Contains("CenterManager", result.Response.User.Roles);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(result.Response.AccessToken);
        Assert.Contains(jwtToken.Claims, c => c.Type == "organization_id" && c.Value == orgId.ToString());
        Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Role && c.Value == "CenterManager");
    }

    [Fact]
    public async Task VerifyOtp_IncorrectCode_FailsAndIncrementsAttempts()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);
        var otpRepo = new OtpCodeRepository(dbContext);
        var userRoleRepo = new UserRoleRepository(dbContext);
        var tokenService = new JwtTokenService(_jwtOptions);

        var testUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Reza",
            LastName = "Khosravi",
            PhoneNumber = "09124445566",
            Email = "reza@ariahr.com",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        await dbContext.Users.AddAsync(testUser);

        var otpCode = new OtpCode
        {
            Id = Guid.NewGuid(),
            UserId = testUser.Id,
            PhoneNumber = "09124445566",
            CodeHash = tokenService.HashToken("1111"),
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(2),
            IsUsed = false,
            AttemptCount = 0
        };
        await dbContext.OtpCodes.AddAsync(otpCode);
        await dbContext.SaveChangesAsync();

        var verifyOtpUseCase = new VerifyOtpUseCase(userRepo, otpRepo, userRoleRepo, tokenService, _otpOptions);

        // Act
        var result = await verifyOtpUseCase.ExecuteAsync(new VerifyOtpRequest("09124445566", "9999"));

        // Assert
        Assert.False(result.Success);
        Assert.Equal("INVALID_CODE", result.ErrorType);

        var updatedOtp = await dbContext.OtpCodes.FirstAsync(o => o.Id == otpCode.Id);
        Assert.Equal(1, updatedOtp.AttemptCount);
    }

    [Fact]
    public async Task VerifyOtp_ExpiredCode_Fails()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);
        var otpRepo = new OtpCodeRepository(dbContext);
        var userRoleRepo = new UserRoleRepository(dbContext);
        var tokenService = new JwtTokenService(_jwtOptions);

        var testUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Maryam",
            LastName = "Nouri",
            PhoneNumber = "09125556677",
            Email = "maryam@ariahr.com",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        await dbContext.Users.AddAsync(testUser);

        var otpCode = new OtpCode
        {
            Id = Guid.NewGuid(),
            UserId = testUser.Id,
            PhoneNumber = "09125556677",
            CodeHash = tokenService.HashToken("2222"),
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-5), // Expired
            IsUsed = false,
            AttemptCount = 0
        };
        await dbContext.OtpCodes.AddAsync(otpCode);
        await dbContext.SaveChangesAsync();

        var verifyOtpUseCase = new VerifyOtpUseCase(userRepo, otpRepo, userRoleRepo, tokenService, _otpOptions);

        // Act
        var result = await verifyOtpUseCase.ExecuteAsync(new VerifyOtpRequest("09125556677", "2222"));

        // Assert
        Assert.False(result.Success);
        Assert.Equal("EXPIRED_CODE", result.ErrorType);
    }

    [Fact]
    public async Task VerifyOtp_AlreadyUsedCode_Fails()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);
        var otpRepo = new OtpCodeRepository(dbContext);
        var userRoleRepo = new UserRoleRepository(dbContext);
        var tokenService = new JwtTokenService(_jwtOptions);

        var testUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Kaveh",
            LastName = "Rad",
            PhoneNumber = "09126667788",
            Email = "kaveh@ariahr.com",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        await dbContext.Users.AddAsync(testUser);

        var otpCode = new OtpCode
        {
            Id = Guid.NewGuid(),
            UserId = testUser.Id,
            PhoneNumber = "09126667788",
            CodeHash = tokenService.HashToken("3333"),
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(2),
            IsUsed = true, // Already used
            AttemptCount = 0
        };
        await dbContext.OtpCodes.AddAsync(otpCode);
        await dbContext.SaveChangesAsync();

        var verifyOtpUseCase = new VerifyOtpUseCase(userRepo, otpRepo, userRoleRepo, tokenService, _otpOptions);

        // Act
        var result = await verifyOtpUseCase.ExecuteAsync(new VerifyOtpRequest("09126667788", "3333"));

        // Assert
        Assert.False(result.Success);
        Assert.Equal("INVALID_CODE", result.ErrorType);
    }

    [Fact]
    public async Task VerifyOtp_MaxAttemptsExceeded_Fails()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);
        var otpRepo = new OtpCodeRepository(dbContext);
        var userRoleRepo = new UserRoleRepository(dbContext);
        var tokenService = new JwtTokenService(_jwtOptions);

        var testUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Nima",
            LastName = "Aria",
            PhoneNumber = "09127778899",
            Email = "nima@ariahr.com",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        await dbContext.Users.AddAsync(testUser);

        var otpCode = new OtpCode
        {
            Id = Guid.NewGuid(),
            UserId = testUser.Id,
            PhoneNumber = "09127778899",
            CodeHash = tokenService.HashToken("4444"),
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(2),
            IsUsed = false,
            AttemptCount = 5 // Max attempts reached
        };
        await dbContext.OtpCodes.AddAsync(otpCode);
        await dbContext.SaveChangesAsync();

        var verifyOtpUseCase = new VerifyOtpUseCase(userRepo, otpRepo, userRoleRepo, tokenService, _otpOptions);

        // Act
        var result = await verifyOtpUseCase.ExecuteAsync(new VerifyOtpRequest("09127778899", "4444"));

        // Assert
        Assert.False(result.Success);
        Assert.Equal("MAX_ATTEMPTS_EXCEEDED", result.ErrorType);
    }

    [Fact]
    public async Task LogoutUseCase_RevokesAllActiveTokensForUser()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var refreshTokenRepo = new RefreshTokenRepository(dbContext);
        var logoutUseCase = new LogoutUseCase(refreshTokenRepo);

        var userId = Guid.NewGuid();
        var activeToken1 = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = "hash1",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
            IsRevoked = false
        };
        var activeToken2 = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = "hash2",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(2),
            IsRevoked = false
        };
        var otherUserToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = "hash3",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
            IsRevoked = false
        };

        await dbContext.RefreshTokens.AddRangeAsync(activeToken1, activeToken2, otherUserToken);
        await dbContext.SaveChangesAsync();

        // Act
        await logoutUseCase.ExecuteAsync(userId, CancellationToken.None);

        // Assert
        var token1 = await dbContext.RefreshTokens.FirstAsync(t => t.Id == activeToken1.Id);
        var token2 = await dbContext.RefreshTokens.FirstAsync(t => t.Id == activeToken2.Id);
        var token3 = await dbContext.RefreshTokens.FirstAsync(t => t.Id == otherUserToken.Id);

        Assert.NotNull(token1.RevokedAtUtc);
        Assert.NotNull(token2.RevokedAtUtc);
        Assert.Null(token3.RevokedAtUtc);
    }
}

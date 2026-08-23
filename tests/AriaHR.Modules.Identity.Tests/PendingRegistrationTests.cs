using AriaHR.Modules.Identity.Application.DTOs;
using AriaHR.Modules.Identity.Application.Options;
using AriaHR.Modules.Identity.Application.UseCases.Registration;
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

public class PendingRegistrationTests
{
    private readonly DbContextOptions<IdentityDbContext> _dbContextOptions;
    private readonly IOptions<JwtOptions> _jwtOptions;

    public PendingRegistrationTests()
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
    public async Task Test1_ExistingUser_InitiateRegistration_ReturnsLogin_NoPendingRegistration()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);
        var pendingRepo = new PendingRegistrationRepository(dbContext);
        var tokenService = new JwtTokenService(_jwtOptions);
        var notificationService = new AuthNotificationService(NullLogger<AuthNotificationService>.Instance);

        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "09121112233",
            PhoneNumber = "09121112233",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        await dbContext.Users.AddAsync(existingUser);
        await dbContext.SaveChangesAsync();

        var initiateUseCase = new InitiateRegistrationUseCase(userRepo, pendingRepo, notificationService, tokenService);

        // Act
        var response = await initiateUseCase.ExecuteAsync(new InitiateRegistrationRequest("09121112233"));

        // Assert
        Assert.True(response.Success);
        Assert.Equal("login", response.NextStep);

        var pending = await dbContext.PendingRegistrations.FirstOrDefaultAsync(p => p.MobileNumber == "09121112233");
        Assert.Null(pending);
    }

    [Fact]
    public async Task Test2_NewUser_InitiateRegistration_CreatesPendingRegistration_StoresOtpHash()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);
        var pendingRepo = new PendingRegistrationRepository(dbContext);
        var tokenService = new JwtTokenService(_jwtOptions);
        var notificationService = new AuthNotificationService(NullLogger<AuthNotificationService>.Instance);

        var initiateUseCase = new InitiateRegistrationUseCase(userRepo, pendingRepo, notificationService, tokenService);

        // Act
        var response = await initiateUseCase.ExecuteAsync(new InitiateRegistrationRequest("09123334455"));

        // Assert
        Assert.True(response.Success);
        Assert.Equal("verify-otp", response.NextStep);

        var pending = await dbContext.PendingRegistrations.FirstOrDefaultAsync(p => p.MobileNumber == "09123334455");
        Assert.NotNull(pending);
        Assert.False(string.IsNullOrWhiteSpace(pending.VerificationCodeHash));
        Assert.Equal(64, pending.VerificationCodeHash.Length); // SHA-256 lower hex string length
        Assert.NotEqual("1234", pending.VerificationCodeHash); // Confirm raw code is not stored plain
    }

    [Fact]
    public async Task Test3_SameNewNumberSubmittedAgain_ReusesPendingRegistration_ResetsAttempts()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);
        var pendingRepo = new PendingRegistrationRepository(dbContext);
        var tokenService = new JwtTokenService(_jwtOptions);
        var notificationService = new AuthNotificationService(NullLogger<AuthNotificationService>.Instance);

        var initiateUseCase = new InitiateRegistrationUseCase(userRepo, pendingRepo, notificationService, tokenService);

        // First attempt
        await initiateUseCase.ExecuteAsync(new InitiateRegistrationRequest("09124445566"));
        var firstPending = await dbContext.PendingRegistrations.FirstOrDefaultAsync(p => p.MobileNumber == "09124445566");
        Assert.NotNull(firstPending);

        string initialHash = firstPending.VerificationCodeHash;
        firstPending.IncrementAttemptCount();
        await pendingRepo.UpdateAsync(firstPending);

        // Act - Second attempt
        var secondResponse = await initiateUseCase.ExecuteAsync(new InitiateRegistrationRequest("09124445566"));

        // Assert
        Assert.True(secondResponse.Success);
        Assert.Equal("verify-otp", secondResponse.NextStep);

        var pendingCount = await dbContext.PendingRegistrations.CountAsync(p => p.MobileNumber == "09124445566");
        Assert.Equal(1, pendingCount); // No duplicate pending records

        var secondPending = await dbContext.PendingRegistrations.FirstOrDefaultAsync(p => p.MobileNumber == "09124445566");
        Assert.NotNull(secondPending);
        Assert.Equal(0, secondPending.AttemptCount); // Attempt count reset
        Assert.False(secondPending.IsVerified);
    }

    [Fact]
    public async Task Test4_CorrectOtp_VerificationSucceeds_CreatesUser_DeletesPendingRegistration()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);
        var pendingRepo = new PendingRegistrationRepository(dbContext);
        var tokenService = new JwtTokenService(_jwtOptions);

        string rawCode = "4821";
        string codeHash = tokenService.HashToken(rawCode);

        var pending = new PendingRegistration
        {
            Id = Guid.NewGuid(),
            MobileNumber = "09125556677",
            VerificationCodeHash = codeHash,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(2),
            CreatedAtUtc = DateTime.UtcNow,
            AttemptCount = 0,
            IsVerified = false
        };
        await dbContext.PendingRegistrations.AddAsync(pending);
        await dbContext.SaveChangesAsync();

        var verifyUseCase = new VerifyRegistrationOtpUseCase(userRepo, pendingRepo, tokenService);

        // Act
        var response = await verifyUseCase.ExecuteAsync(new VerifyRegistrationOtpRequest("09125556677", rawCode));

        // Assert
        Assert.True(response.Success);
        Assert.Equal("login", response.NextStep);

        // User created
        var createdUser = await dbContext.Users.FirstOrDefaultAsync(u => u.PhoneNumber == "09125556677");
        Assert.NotNull(createdUser);
        Assert.Equal("09125556677", createdUser.Username);

        // PendingRegistration deleted
        var remainingPending = await dbContext.PendingRegistrations.FirstOrDefaultAsync(p => p.MobileNumber == "09125556677");
        Assert.Null(remainingPending);
    }

    [Fact]
    public async Task Test5_IncorrectOtp_VerificationFails_IncrementsAttemptCount_UserNotCreated()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);
        var pendingRepo = new PendingRegistrationRepository(dbContext);
        var tokenService = new JwtTokenService(_jwtOptions);

        string correctCode = "1234";
        string codeHash = tokenService.HashToken(correctCode);

        var pending = new PendingRegistration
        {
            Id = Guid.NewGuid(),
            MobileNumber = "09126667788",
            VerificationCodeHash = codeHash,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(2),
            CreatedAtUtc = DateTime.UtcNow,
            AttemptCount = 0,
            IsVerified = false
        };
        await dbContext.PendingRegistrations.AddAsync(pending);
        await dbContext.SaveChangesAsync();

        var verifyUseCase = new VerifyRegistrationOtpUseCase(userRepo, pendingRepo, tokenService);

        // Act
        var response = await verifyUseCase.ExecuteAsync(new VerifyRegistrationOtpRequest("09126667788", "9999"));

        // Assert
        Assert.False(response.Success);

        // Attempt count incremented
        var updatedPending = await dbContext.PendingRegistrations.FirstOrDefaultAsync(p => p.MobileNumber == "09126667788");
        Assert.NotNull(updatedPending);
        Assert.Equal(1, updatedPending.AttemptCount);

        // User not created
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.PhoneNumber == "09126667788");
        Assert.Null(user);
    }

    [Fact]
    public async Task Test6_ExpiredOtp_VerificationRejected_UserNotCreated()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);
        var pendingRepo = new PendingRegistrationRepository(dbContext);
        var tokenService = new JwtTokenService(_jwtOptions);

        string code = "1234";
        string codeHash = tokenService.HashToken(code);

        var pending = new PendingRegistration
        {
            Id = Guid.NewGuid(),
            MobileNumber = "09127778899",
            VerificationCodeHash = codeHash,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-5), // Expired 5 mins ago
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            AttemptCount = 0,
            IsVerified = false
        };
        await dbContext.PendingRegistrations.AddAsync(pending);
        await dbContext.SaveChangesAsync();

        var verifyUseCase = new VerifyRegistrationOtpUseCase(userRepo, pendingRepo, tokenService);

        // Act
        var response = await verifyUseCase.ExecuteAsync(new VerifyRegistrationOtpRequest("09127778899", code));

        // Assert
        Assert.False(response.Success);
        Assert.Equal("otp-expired", response.NextStep);

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.PhoneNumber == "09127778899");
        Assert.Null(user);
    }

    [Fact]
    public async Task Test7_MaximumAttempts_VerificationRejected()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);
        var pendingRepo = new PendingRegistrationRepository(dbContext);
        var tokenService = new JwtTokenService(_jwtOptions);

        string code = "1234";
        string codeHash = tokenService.HashToken(code);

        var pending = new PendingRegistration
        {
            Id = Guid.NewGuid(),
            MobileNumber = "09128889900",
            VerificationCodeHash = codeHash,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(2),
            CreatedAtUtc = DateTime.UtcNow,
            AttemptCount = 5, // Reached max attempts
            IsVerified = false
        };
        await dbContext.PendingRegistrations.AddAsync(pending);
        await dbContext.SaveChangesAsync();

        var verifyUseCase = new VerifyRegistrationOtpUseCase(userRepo, pendingRepo, tokenService, new OtpOptions { MaxAttempts = 5 });

        // Act
        var response = await verifyUseCase.ExecuteAsync(new VerifyRegistrationOtpRequest("09128889900", code));

        // Assert
        Assert.False(response.Success);
        Assert.Equal("max-attempts-exceeded", response.NextStep);

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.PhoneNumber == "09128889900");
        Assert.Null(user);
    }

    [Fact]
    public async Task Test8_ResendOtp_OldOtpRejected_NewOtpAccepted()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var userRepo = new UserRepository(dbContext);
        var pendingRepo = new PendingRegistrationRepository(dbContext);
        var tokenService = new JwtTokenService(_jwtOptions);
        var notificationService = new AuthNotificationService(NullLogger<AuthNotificationService>.Instance);

        var initiateUseCase = new InitiateRegistrationUseCase(userRepo, pendingRepo, notificationService, tokenService);
        var verifyUseCase = new VerifyRegistrationOtpUseCase(userRepo, pendingRepo, tokenService);

        // Act 1: Initial initiate
        await initiateUseCase.ExecuteAsync(new InitiateRegistrationRequest("09129990011"));
        var pending1 = await dbContext.PendingRegistrations.FirstOrDefaultAsync(p => p.MobileNumber == "09129990011");
        Assert.NotNull(pending1);

        // Manually replace with known old code hash
        string oldCode = "1111";
        pending1.VerificationCodeHash = tokenService.HashToken(oldCode);
        await pendingRepo.UpdateAsync(pending1);

        // Act 2: Resend initiate
        await initiateUseCase.ExecuteAsync(new InitiateRegistrationRequest("09129990011"));
        var pending2 = await dbContext.PendingRegistrations.FirstOrDefaultAsync(p => p.MobileNumber == "09129990011");
        Assert.NotNull(pending2);

        // Set known new code hash
        string newCode = "2222";
        pending2.VerificationCodeHash = tokenService.HashToken(newCode);
        await pendingRepo.UpdateAsync(pending2);

        // Act 3: Submit old code -> Should fail
        var oldCodeResponse = await verifyUseCase.ExecuteAsync(new VerifyRegistrationOtpRequest("09129990011", oldCode));
        Assert.False(oldCodeResponse.Success);

        // Act 4: Submit new code -> Should succeed
        var newCodeResponse = await verifyUseCase.ExecuteAsync(new VerifyRegistrationOtpRequest("09129990011", newCode));
        Assert.True(newCodeResponse.Success);
        Assert.Equal("login", newCodeResponse.NextStep);
    }
}

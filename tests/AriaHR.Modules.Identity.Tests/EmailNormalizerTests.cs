using AriaHR.Modules.Identity.Application.Common;
using AriaHR.Modules.Identity.Domain.Entities;
using AriaHR.Modules.Identity.Infrastructure.Diagnostics;
using AriaHR.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AriaHR.Modules.Identity.Tests;

public class EmailNormalizerTests
{
    [Theory]
    [InlineData("User.Name@Domain.Com", "user.name@domain.com")]
    [InlineData("  TEST.EMAIL@EXAMPLE.ORG  ", "test.email@example.org")]
    [InlineData("simple@example.com", "simple@example.com")]
    public void Normalize_WithValidEmail_ReturnsTrimmedLowercase(string input, string expected)
    {
        var result = EmailNormalizer.Normalize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Normalize_WithNullOrWhitespace_ReturnsNull(string? input)
    {
        var result = EmailNormalizer.Normalize(input);
        Assert.Null(result);
    }

    [Fact]
    public async Task UserEmailDuplicateChecker_DetectsDuplicateNormalizedEmails()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new IdentityDbContext(options);

        // Add 2 active users with same normalized email
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            FirstName = "User1",
            LastName = "Test",
            PhoneNumber = "09120000001",
            Email = "DUP.EMAIL@EXAMPLE.COM",
            IsActive = true,
            IsDeleted = false
        });

        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            FirstName = "User2",
            LastName = "Test",
            PhoneNumber = "09120000002",
            Email = "dup.email@example.com",
            IsActive = true,
            IsDeleted = false
        });

        // Add soft-deleted user with same email (should NOT trigger conflict)
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            FirstName = "User3",
            LastName = "Deleted",
            PhoneNumber = "09120000003",
            Email = "dup.email@example.com",
            IsActive = false,
            IsDeleted = true
        });

        await db.SaveChangesAsync();

        var report = await UserEmailDuplicateChecker.CheckForDuplicateEmailsAsync(db);

        Assert.True(report.HasConflicts);
        Assert.Single(report.Conflicts);
        Assert.Equal("dup.email@example.com", report.Conflicts[0].NormalizedEmail);
        Assert.Equal(2, report.Conflicts[0].UserIds.Count);
    }
}

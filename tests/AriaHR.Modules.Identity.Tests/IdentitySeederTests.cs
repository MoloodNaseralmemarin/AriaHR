using AriaHR.Modules.Identity.Domain.Entities;
using AriaHR.Modules.Identity.Infrastructure.Persistence;
using AriaHR.Modules.Identity.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AriaHR.Modules.Identity.Tests;

public class IdentitySeederTests
{
    private readonly DbContextOptions<IdentityDbContext> _dbContextOptions;

    public IdentitySeederTests()
    {
        _dbContextOptions = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    private IdentityDbContext CreateDbContext() => new(_dbContextOptions);

    [Fact]
    public async Task SeedAsync_Creates_Initial_System_Roles()
    {
        // Arrange
        using var dbContext = CreateDbContext();

        // Act
        await IdentitySeeder.SeedAsync(dbContext);

        // Assert
        var roles = await dbContext.Roles.ToListAsync();
        Assert.Equal(3, roles.Count);
        Assert.Contains(roles, r => r.Name == "SystemAdmin");
        Assert.Contains(roles, r => r.Name == "CenterManager");
        Assert.Contains(roles, r => r.Name == "Employee");

        foreach (var role in roles)
        {
            Assert.Null(role.Description);
            Assert.NotEqual(default, role.CreatedAtUtc);
        }
    }

    [Fact]
    public async Task SeedAsync_Is_Idempotent()
    {
        // Arrange
        using var dbContext = CreateDbContext();

        // Act - Run 1
        await IdentitySeeder.SeedAsync(dbContext);
        var initialRoles = await dbContext.Roles.ToListAsync();
        Assert.Equal(3, initialRoles.Count);

        // Act - Run 2
        await IdentitySeeder.SeedAsync(dbContext);
        var subsequentRoles = await dbContext.Roles.ToListAsync();

        // Assert
        Assert.Equal(3, subsequentRoles.Count);
    }

    [Fact]
    public async Task SeedAsync_Only_Creates_Missing_Roles()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var existingRole = new Role
        {
            Name = "SystemAdmin",
            Description = null,
            CreatedAtUtc = DateTime.UtcNow
        };
        await dbContext.Roles.AddAsync(existingRole);
        await dbContext.SaveChangesAsync();

        // Act
        await IdentitySeeder.SeedAsync(dbContext);

        // Assert
        var roles = await dbContext.Roles.ToListAsync();
        Assert.Equal(3, roles.Count);
        Assert.Contains(roles, r => r.Name == "SystemAdmin");
        Assert.Contains(roles, r => r.Name == "CenterManager");
        Assert.Contains(roles, r => r.Name == "Employee");
    }
}

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
    public async Task SeedAsync_Creates_Initial_System_Roles_With_Descriptions()
    {
        // Arrange
        using var dbContext = CreateDbContext();

        // Act
        await IdentitySeeder.SeedAsync(dbContext);

        // Assert
        var roles = await dbContext.Roles.ToListAsync();
        Assert.Equal(3, roles.Count);

        var admin = roles.FirstOrDefault(r => r.Name == "SystemAdmin");
        Assert.NotNull(admin);
        Assert.Equal("System Administrator", admin.Description);
        Assert.NotEqual(default, admin.CreatedAtUtc);

        var manager = roles.FirstOrDefault(r => r.Name == "CenterManager");
        Assert.NotNull(manager);
        Assert.Equal("Center Manager", manager.Description);
        Assert.NotEqual(default, manager.CreatedAtUtc);

        var employee = roles.FirstOrDefault(r => r.Name == "Employee");
        Assert.NotNull(employee);
        Assert.Equal("Employee", employee.Description);
        Assert.NotEqual(default, employee.CreatedAtUtc);
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
            Description = "System Administrator",
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

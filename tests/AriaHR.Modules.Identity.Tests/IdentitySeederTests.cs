using AriaHR.Modules.Identity.Domain.Entities;
using AriaHR.Modules.Identity.Infrastructure.Persistence;
using AriaHR.Modules.Identity.Infrastructure.Seed;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    public async Task SeedAsync_Is_Idempotent_For_Roles()
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
    public async Task SeedAsync_DoesNot_Modify_Existing_Role_Descriptions()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var customAdminRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = "SystemAdmin",
            Description = "Custom Pre-existing Description",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
        };
        await dbContext.Roles.AddAsync(customAdminRole);
        await dbContext.SaveChangesAsync();

        // Act
        await IdentitySeeder.SeedAsync(dbContext);

        // Assert
        var adminRole = await dbContext.Roles.FirstAsync(r => r.Name == "SystemAdmin");
        Assert.Equal("Custom Pre-existing Description", adminRole.Description);

        var totalRoles = await dbContext.Roles.CountAsync();
        Assert.Equal(3, totalRoles);
    }

    [Fact]
    public async Task SeedAsync_Creates_Configured_Initial_SystemAdmin_Users_And_UserRoles()
    {
        // Arrange
        using var dbContext = CreateDbContext();

        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Identity:InitialAdmins:0:FirstName", "System"},
            {"Identity:InitialAdmins:0:LastName", "Administrator"},
            {"Identity:InitialAdmins:0:PhoneNumber", "09120000001"},
            {"Identity:InitialAdmins:1:FirstName", "System"},
            {"Identity:InitialAdmins:1:LastName", "Administrator"},
            {"Identity:InitialAdmins:1:PhoneNumber", "09120000002"}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        await IdentitySeeder.SeedAsync(dbContext, configuration);

        // Assert
        var users = await dbContext.Users.ToListAsync();
        Assert.Equal(2, users.Count);

        var adminRole = await dbContext.Roles.FirstAsync(r => r.Name == "SystemAdmin");

        foreach (var user in users)
        {
            Assert.True(user.IsActive);
            Assert.Equal("System", user.FirstName);
            Assert.Equal("Administrator", user.LastName);
            Assert.NotEqual(default, user.CreatedAtUtc);

            var userRole = await dbContext.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == adminRole.Id);
            Assert.NotNull(userRole);
        }
    }

    [Fact]
    public async Task SeedAsync_Is_Idempotent_For_Initial_SystemAdmin_Users()
    {
        // Arrange
        using var dbContext = CreateDbContext();

        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Identity:InitialAdmins:0:FirstName", "System"},
            {"Identity:InitialAdmins:0:LastName", "Administrator"},
            {"Identity:InitialAdmins:0:PhoneNumber", "09120000001"},
            {"Identity:InitialAdmins:1:FirstName", "System"},
            {"Identity:InitialAdmins:1:LastName", "Administrator"},
            {"Identity:InitialAdmins:1:PhoneNumber", "09120000002"}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act - Run 1
        await IdentitySeeder.SeedAsync(dbContext, configuration);
        var usersRun1 = await dbContext.Users.ToListAsync();
        var userRolesRun1 = await dbContext.UserRoles.ToListAsync();

        Assert.Equal(2, usersRun1.Count);
        Assert.Equal(2, userRolesRun1.Count);

        // Act - Run 2
        await IdentitySeeder.SeedAsync(dbContext, configuration);
        var usersRun2 = await dbContext.Users.ToListAsync();
        var userRolesRun2 = await dbContext.UserRoles.ToListAsync();

        // Assert
        Assert.Equal(2, usersRun2.Count);
        Assert.Equal(2, userRolesRun2.Count);
    }

    [Fact]
    public async Task SeedAsync_Persists_Roles_With_Descriptions_In_Relational_Database()
    {
        // Arrange
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlite(connection)
            .Options;

        using (var dbContext = new IdentityDbContext(options))
        {
            await dbContext.Database.EnsureCreatedAsync();
            await IdentitySeeder.SeedAsync(dbContext);
        }

        // Act & Assert - Query using a separate DbContext instance
        using (var dbContext = new IdentityDbContext(options))
        {
            var roles = await dbContext.Roles.AsNoTracking().ToListAsync();
            Assert.Equal(3, roles.Count);

            var systemAdmin = roles.FirstOrDefault(r => r.Name == "SystemAdmin");
            Assert.NotNull(systemAdmin);
            Assert.Equal("System Administrator", systemAdmin.Description);

            var centerManager = roles.FirstOrDefault(r => r.Name == "CenterManager");
            Assert.NotNull(centerManager);
            Assert.Equal("Center Manager", centerManager.Description);

            var employee = roles.FirstOrDefault(r => r.Name == "Employee");
            Assert.NotNull(employee);
            Assert.Equal("Employee", employee.Description);
        }
    }
}

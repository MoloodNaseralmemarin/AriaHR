using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using AriaHR.Modules.Organization.Domain.Entities;
using AriaHR.Modules.Organization.Domain.Enums;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using AriaHR.Modules.Identity.Infrastructure.Persistence;
using AriaHR.Modules.Attendance.Infrastructure.Persistence;
using AriaHR.Modules.Scheduling.Infrastructure.Persistence;
using AriaHR.Modules.Requests.Infrastructure.Persistence;
using AriaHR.Modules.Notification.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace AriaHR.Modules.Organization.Tests;

public class OrganizationApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OrganizationApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var dbName = Guid.NewGuid().ToString();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var efServiceProvider = new ServiceCollection()
                    .AddEntityFrameworkInMemoryDatabase()
                    .BuildServiceProvider();

                services.RemoveAll(typeof(DbContextOptions<OrganizationDbContext>));
                services.RemoveAll(typeof(DbContextOptions<IdentityDbContext>));
                services.RemoveAll(typeof(DbContextOptions<AttendanceDbContext>));
                services.RemoveAll(typeof(DbContextOptions<SchedulingDbContext>));
                services.RemoveAll(typeof(DbContextOptions<RequestsDbContext>));
                services.RemoveAll(typeof(DbContextOptions<NotificationDbContext>));
                services.RemoveAll(typeof(DbContextOptions));

                services.AddDbContext<OrganizationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                    options.UseInternalServiceProvider(efServiceProvider);
                });
                services.AddDbContext<IdentityDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                    options.UseInternalServiceProvider(efServiceProvider);
                });
                services.AddDbContext<AttendanceDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                    options.UseInternalServiceProvider(efServiceProvider);
                });
                services.AddDbContext<SchedulingDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                    options.UseInternalServiceProvider(efServiceProvider);
                });
                services.AddDbContext<RequestsDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                    options.UseInternalServiceProvider(efServiceProvider);
                });
                services.AddDbContext<NotificationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                    options.UseInternalServiceProvider(efServiceProvider);
                });
            });
        });
    }

    private string GenerateTestJwtToken(string role = "SystemAdmin")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes("YOUR_DEV_SECRET_KEY_MUST_BE_AT_LEAST_256_BITS_LONG_FOR_SECURITY_CHANGEME");
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, role)
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = "AriaHR.API",
            Audience = "AriaHR.Clients",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    [Fact]
    public async Task GetOrganizations_ReturnsJsonWithManagerFullNameManagerMobileAndCenterPhone()
    {
        // Arrange
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
            var testOrg = new Domain.Entities.Organization
            {
                Id = Guid.NewGuid(),
                Name = "مرکز تخصصی آریا",
                Code = "ARIA01",
                OrganizationType = OrganizationType.Clinic,
                ManagerFirstName = "امیر",
                ManagerLastName = "حسینی",
                ManagerMobile = "09121112233",
                Phone = "02188776655",
                IsActive = true,
                CreatedAtUtc = new DateTime(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc)
            };

            db.Organizations.Add(testOrg);
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        var token = GenerateTestJwtToken("SystemAdmin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/organizations");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();

        Assert.Contains("managerFullName", json);
        Assert.Contains("امیر حسینی", json);
        Assert.Contains("managerMobile", json);
        Assert.Contains("09121112233", json);
        Assert.Contains("centerPhone", json);
        Assert.Contains("02188776655", json);
    }
}

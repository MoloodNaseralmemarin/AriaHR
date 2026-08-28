using System.Reflection;
using System.Security.Claims;
using AriaHR.Modules.Identity.Domain.Entities;
using AriaHR.Modules.Identity.Infrastructure.Persistence;
using AriaHR.Modules.Organization.API.Controllers;
using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.UseCases.CreateOrganization;
using AriaHR.Modules.Organization.Domain.Entities;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using AriaHR.Modules.Organization.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AriaHR.Modules.Organization.Tests;

public class OrganizationCreateTests
{
    private (OrganizationDbContext orgDb, IdentityDbContext identityDb) GetInMemoryDbContexts()
    {
        string dbName = Guid.NewGuid().ToString();

        var orgOptions = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var identityOptions = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var orgDb = new OrganizationDbContext(orgOptions);
        var identityDb = new IdentityDbContext(identityOptions);

        identityDb.Roles.AddRange(
            new Role { Id = Guid.NewGuid(), Name = "SystemAdmin", Description = "System Administrator" },
            new Role { Id = Guid.NewGuid(), Name = "CenterManager", Description = "Center Manager" },
            new Role { Id = Guid.NewGuid(), Name = "Employee", Description = "Employee" }
        );
        identityDb.SaveChanges();

        return (orgDb, identityDb);
    }

    [Theory]
    [InlineData(OrganizationType.Clinic)]
    [InlineData(OrganizationType.ImagingCenter)]
    [InlineData(OrganizationType.MedicalOffice)]
    [InlineData(OrganizationType.Laboratory)]
    [InlineData(OrganizationType.Pharmacy)]
    public async Task ExecuteAsync_WithValidRequest_CreatesOrganizationUserAndCenterManagerRole(OrganizationType orgType)
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        var managerIdentityService = new OrganizationManagerIdentityService(orgDb, identityDb);
        var useCase = new CreateOrganizationUseCase(managerIdentityService);

        var expectedCreatorId = Guid.NewGuid();
        var request = new CreateOrganizationRequest
        {
            Name = $"Aria Health Center {orgType}",
            Code = $"ORG-{orgType}",
            Type = orgType,
            NationalIdentifier = "1234567890",
            Phone = "+123456789",
            Address = "123 Health St",
            ManagerFirstName = "Ali",
            ManagerLastName = "Ahmadi",
            ManagerMobile = "09123456789",
            IsActive = true
        };

        // Act
        var result = await useCase.ExecuteAsync(request, expectedCreatorId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal($"Aria Health Center {orgType}", result.Name);
        Assert.Equal($"ORG-{orgType}", result.Code);
        Assert.Equal(orgType, result.Type);
        Assert.Equal("Ali", result.ManagerFirstName);
        Assert.Equal("Ahmadi", result.ManagerLastName);
        Assert.Equal("09123456789", result.ManagerMobile);

        // Verify Organization persistence
        var dbOrg = await orgDb.Organizations.FirstOrDefaultAsync(o => o.Id == result.Id);
        Assert.NotNull(dbOrg);
        Assert.Equal("Ali", dbOrg.ManagerFirstName);
        Assert.Equal("Ahmadi", dbOrg.ManagerLastName);
        Assert.Equal("09123456789", dbOrg.ManagerMobile);

        // Verify User persistence and field mapping
        var dbUser = await identityDb.Users.FirstOrDefaultAsync(u => u.PhoneNumber == "09123456789");
        Assert.NotNull(dbUser);
        Assert.Equal("Ali", dbUser.FirstName);
        Assert.Equal("Ahmadi", dbUser.LastName);
        Assert.Equal("09123456789", dbUser.PhoneNumber);
        Assert.Null(dbUser.Email);
        Assert.True(dbUser.IsActive);

        // Verify UserRole persistence and CenterManager role assignment
        var centerManagerRole = await identityDb.Roles.FirstAsync(r => r.Name == "CenterManager");
        var systemAdminRole = await identityDb.Roles.FirstAsync(r => r.Name == "SystemAdmin");

        var dbUserRole = await identityDb.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == dbUser.Id);
        Assert.NotNull(dbUserRole);
        Assert.Equal(centerManagerRole.Id, dbUserRole.RoleId);
        Assert.NotEqual(systemAdminRole.Id, dbUserRole.RoleId);
    }

    [Fact]
    public async Task ExecuteAsync_WithDuplicateManagerMobile_ThrowsArgumentExceptionAndCreatesNothing()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();

        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Existing",
            LastName = "User",
            PhoneNumber = "09123456789",
            IsActive = true
        };
        identityDb.Users.Add(existingUser);
        identityDb.SaveChanges();

        var managerIdentityService = new OrganizationManagerIdentityService(orgDb, identityDb);
        var useCase = new CreateOrganizationUseCase(managerIdentityService);

        var request = new CreateOrganizationRequest
        {
            Name = "Duplicate Mobile Center",
            Code = "DUP-001",
            Type = OrganizationType.Clinic,
            ManagerFirstName = "Ali",
            ManagerLastName = "Ahmadi",
            ManagerMobile = "09123456789"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, Guid.NewGuid()));
        Assert.Contains("mobile number already exists", ex.Message, StringComparison.OrdinalIgnoreCase);

        // Assert no Organization was created
        Assert.Empty(orgDb.Organizations);

        // Assert no new User or UserRole was created
        Assert.Single(identityDb.Users);
        Assert.Empty(identityDb.UserRoles);
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingManagerFirstName_ThrowsArgumentException()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        var managerIdentityService = new OrganizationManagerIdentityService(orgDb, identityDb);
        var useCase = new CreateOrganizationUseCase(managerIdentityService);

        var request = new CreateOrganizationRequest
        {
            Name = "Valid Name",
            Code = "CODE-01",
            Type = OrganizationType.Clinic,
            ManagerFirstName = "",
            ManagerLastName = "Ahmadi",
            ManagerMobile = "09123456789"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, Guid.NewGuid()));
        Assert.Empty(orgDb.Organizations);
        Assert.Empty(identityDb.Users);
    }

    [Fact]
    public async Task Controller_Create_WithValidSystemAdminClaims_Returns201Created()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        var managerIdentityService = new OrganizationManagerIdentityService(orgDb, identityDb);
        var useCase = new CreateOrganizationUseCase(managerIdentityService);
        var controller = new OrganizationsController(useCase);

        var expectedUserId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, expectedUserId.ToString()),
            new Claim(ClaimTypes.Role, "SystemAdmin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var request = new CreateOrganizationRequest
        {
            Name = "SystemAdmin Org",
            Code = "SA-001",
            Type = OrganizationType.ImagingCenter,
            ManagerFirstName = "Ali",
            ManagerLastName = "Ahmadi",
            ManagerMobile = "09123456789"
        };

        // Act
        var actionResult = await controller.Create(request, CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);

        var dto = Assert.IsType<OrganizationDto>(objectResult.Value);
        Assert.Equal("SystemAdmin Org", dto.Name);
        Assert.Equal(OrganizationType.ImagingCenter, dto.Type);
        Assert.Equal(expectedUserId, dto.CreatedByUserId);
    }

    [Fact]
    public async Task Controller_Create_WithDuplicateMobile_Returns400BadRequest()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();

        identityDb.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Existing",
            LastName = "User",
            PhoneNumber = "09123456789"
        });
        identityDb.SaveChanges();

        var managerIdentityService = new OrganizationManagerIdentityService(orgDb, identityDb);
        var useCase = new CreateOrganizationUseCase(managerIdentityService);
        var controller = new OrganizationsController(useCase);

        var expectedUserId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, expectedUserId.ToString()),
            new Claim(ClaimTypes.Role, "SystemAdmin")
        };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) }
        };

        var request = new CreateOrganizationRequest
        {
            Name = "Org Dup Mobile",
            Code = "DUP-002",
            Type = OrganizationType.Laboratory,
            ManagerFirstName = "Ali",
            ManagerLastName = "Ahmadi",
            ManagerMobile = "09123456789"
        };

        // Act
        var actionResult = await controller.Create(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);

        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problemDetails.Status);
        Assert.Contains("mobile number already exists", problemDetails.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Controller_Create_WithNullRequest_Returns400BadRequest()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        var managerIdentityService = new OrganizationManagerIdentityService(orgDb, identityDb);
        var useCase = new CreateOrganizationUseCase(managerIdentityService);
        var controller = new OrganizationsController(useCase);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "SystemAdmin")
        };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) }
        };

        // Act
        var actionResult = await controller.Create(null!, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task Controller_Create_WithoutUserIdClaim_Returns401Unauthorized()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        var managerIdentityService = new OrganizationManagerIdentityService(orgDb, identityDb);
        var useCase = new CreateOrganizationUseCase(managerIdentityService);
        var controller = new OrganizationsController(useCase);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
        };

        var request = new CreateOrganizationRequest
        {
            Name = "Unauthorized Org",
            Code = "UA-001",
            Type = OrganizationType.Clinic,
            ManagerFirstName = "Ali",
            ManagerLastName = "Ahmadi",
            ManagerMobile = "09123456789"
        };

        // Act
        var actionResult = await controller.Create(request, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedResult>(actionResult);
    }

    [Fact]
    public void Controller_HasAuthorizeAttribute_RestrictedToSystemAdmin()
    {
        // Arrange & Act
        var controllerType = typeof(OrganizationsController);
        var authorizeAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        // Assert
        Assert.NotNull(authorizeAttr);
        Assert.Equal("SystemAdmin", authorizeAttr.Roles);
    }
}

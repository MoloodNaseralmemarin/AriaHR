using System.Reflection;
using System.Security.Claims;
using AriaHR.Modules.Identity.Domain.Entities;
using AriaHR.Modules.Identity.Infrastructure.Persistence;
using AriaHR.Modules.Identity.Infrastructure.Repositories;
using AriaHR.Modules.Organization.API.Controllers;
using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.UseCases.CreateOrganization;
using AriaHR.Modules.Organization.Application.UseCases.GetDashboardSummary;
using AriaHR.Modules.Organization.Application.UseCases.GetRecentOrganizations;
using AriaHR.Modules.Organization.Application.UseCases.GetTotalOrganizationsCount;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using OrganizationEntity = AriaHR.Modules.Organization.Domain.Entities.Organization;
using OrganizationType = AriaHR.Modules.Organization.Domain.Enums.OrganizationType;
using AriaHR.Modules.Organization.Infrastructure.Repositories;
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
            ManagerEmail = "ali.ahmadi@clinic.com",
            IsActive = true
        };

        // Act
        var result = await useCase.ExecuteAsync(request, expectedCreatorId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal($"Aria Health Center {orgType}", result.Name);
        Assert.Equal($"ORG-{orgType}", result.Code);
        Assert.Equal(orgType, result.OrganizationType);
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
        Assert.Equal("ali.ahmadi@clinic.com", dbUser.Email);
        Assert.Equal(result.Id, dbUser.OrganizationId);
        Assert.True(dbUser.IsActive);

        // Verify UserRole persistence and CenterManager role assignment
        var centerManagerRole = await identityDb.Roles.FirstAsync(r => r.Name == "CenterManager");
        var systemAdminRole = await identityDb.Roles.FirstAsync(r => r.Name == "SystemAdmin");

        var dbUserRole = await identityDb.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == dbUser.Id);
        Assert.NotNull(dbUserRole);
        Assert.Equal(centerManagerRole.Id, dbUserRole.RoleId);
        Assert.NotEqual(systemAdminRole.Id, dbUserRole.RoleId);

        // Verify no Branch record is required or created
        Assert.Empty(orgDb.Branches);
    }

    [Fact]
    public async Task ExecuteAsync_WithExistingUserNoOrganization_AttachesUserToOrganizationAndUpdatesEmail()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();

        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Unassigned",
            LastName = "User",
            PhoneNumber = "09123456789",
            Email = "old.email@clinic.com",
            OrganizationId = null,
            IsActive = true
        };
        identityDb.Users.Add(existingUser);
        identityDb.SaveChanges();

        var managerIdentityService = new OrganizationManagerIdentityService(orgDb, identityDb);
        var useCase = new CreateOrganizationUseCase(managerIdentityService);

        var request = new CreateOrganizationRequest
        {
            Name = "New Center",
            Code = "NC-001",
            Type = OrganizationType.Clinic,
            ManagerFirstName = "UpdatedFirstName",
            ManagerLastName = "UpdatedLastName",
            ManagerMobile = "09123456789",
            ManagerEmail = "new.email@clinic.com"
        };

        // Act
        var result = await useCase.ExecuteAsync(request, Guid.NewGuid());

        // Assert
        Assert.NotNull(result);
        Assert.Single(identityDb.Users); // No duplicate user created
        var dbUser = await identityDb.Users.FirstAsync();
        Assert.Equal(result.Id, dbUser.OrganizationId);
        Assert.Equal("UpdatedFirstName", dbUser.FirstName);
        Assert.Equal("UpdatedLastName", dbUser.LastName);
        Assert.Equal("new.email@clinic.com", dbUser.Email);

        var centerManagerRole = await identityDb.Roles.FirstAsync(r => r.Name == "CenterManager");
        var userRole = await identityDb.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == dbUser.Id && ur.RoleId == centerManagerRole.Id);
        Assert.NotNull(userRole);
    }

    [Fact]
    public async Task ExecuteAsync_WithExistingUserMatchingSameEmail_SucceedsWithoutDuplicateError()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();

        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Ali",
            LastName = "Ahmadi",
            PhoneNumber = "09123456789",
            Email = "ali.ahmadi@clinic.com",
            OrganizationId = null,
            IsActive = true
        };
        identityDb.Users.Add(existingUser);
        identityDb.SaveChanges();

        var managerIdentityService = new OrganizationManagerIdentityService(orgDb, identityDb);
        var useCase = new CreateOrganizationUseCase(managerIdentityService);

        var request = new CreateOrganizationRequest
        {
            Name = "Aria Clinic",
            Code = "AC-001",
            Type = OrganizationType.Clinic,
            ManagerFirstName = "Ali",
            ManagerLastName = "Ahmadi",
            ManagerMobile = "09123456789",
            ManagerEmail = "ali.ahmadi@clinic.com"
        };

        // Act
        var result = await useCase.ExecuteAsync(request, Guid.NewGuid());

        // Assert
        Assert.NotNull(result);
        var dbUser = await identityDb.Users.FirstAsync(u => u.Id == existingUser.Id);
        Assert.Equal("ali.ahmadi@clinic.com", dbUser.Email);
    }

    [Fact]
    public async Task ExecuteAsync_WithDuplicateEmailForAnotherUser_ThrowsArgumentException()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();

        var otherUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Other",
            LastName = "Manager",
            PhoneNumber = "09111111111",
            Email = "existing.manager@clinic.com",
            IsActive = true
        };
        identityDb.Users.Add(otherUser);
        identityDb.SaveChanges();

        var managerIdentityService = new OrganizationManagerIdentityService(orgDb, identityDb);
        var useCase = new CreateOrganizationUseCase(managerIdentityService);

        var request = new CreateOrganizationRequest
        {
            Name = "New Org",
            Code = "NEW-01",
            Type = OrganizationType.Clinic,
            ManagerFirstName = "Ali",
            ManagerLastName = "Ahmadi",
            ManagerMobile = "09222222222",
            ManagerEmail = "EXISTING.MANAGER@CLINIC.COM" // duplicate case-insensitive
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, Guid.NewGuid()));
        Assert.Contains("این ایمیل قبلاً برای کاربر دیگری ثبت شده است.", ex.Message);
        Assert.Empty(orgDb.Organizations);
    }

    [Fact]
    public async Task ExecuteAsync_WithExistingUserOtherOrganization_ThrowsArgumentException()
    {
        // Arrange (Test 4: Existing User Belongs to Another Organization)
        var (orgDb, identityDb) = GetInMemoryDbContexts();

        var orgAId = Guid.NewGuid();
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "OtherOrg",
            LastName = "Manager",
            PhoneNumber = "09123456789",
            Email = "manager.orga@clinic.com",
            OrganizationId = orgAId,
            IsActive = true
        };
        identityDb.Users.Add(existingUser);
        identityDb.SaveChanges();

        var managerIdentityService = new OrganizationManagerIdentityService(orgDb, identityDb);
        var useCase = new CreateOrganizationUseCase(managerIdentityService);

        var request = new CreateOrganizationRequest
        {
            Name = "Organization B",
            Code = "ORGB",
            Type = OrganizationType.Clinic,
            ManagerFirstName = "Ali",
            ManagerLastName = "Ahmadi",
            ManagerMobile = "09123456789",
            ManagerEmail = "manager.orgb@clinic.com"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, Guid.NewGuid()));
        Assert.Contains("کاربری با این شماره موبایل قبلاً به سازمان دیگری اختصاص یافته است.", ex.Message);

        // Verify User's OrganizationId was NOT changed
        var dbUser = await identityDb.Users.FirstAsync(u => u.Id == existingUser.Id);
        Assert.Equal(orgAId, dbUser.OrganizationId);
    }

    [Theory]
    [InlineData("", "CODE", "Ali", "Ahmadi", "09123456789", "ali@clinic.com", "نام سازمان الزامی است.")]
    [InlineData("Name", "", "Ali", "Ahmadi", "09123456789", "ali@clinic.com", "کد سازمان الزامی است.")]
    [InlineData("Name", "CODE", "", "Ahmadi", "09123456789", "ali@clinic.com", "نام مدیر الزامی است.")]
    [InlineData("Name", "CODE", "Ali", "", "09123456789", "ali@clinic.com", "نام خانوادگی مدیر الزامی است.")]
    [InlineData("Name", "CODE", "Ali", "Ahmadi", "", "ali@clinic.com", "شماره موبایل مدیر الزامی است.")]
    [InlineData("Name", "CODE", "Ali", "Ahmadi", "09123456789", "", "ایمیل مدیر مرکز الزامی است.")]
    [InlineData("Name", "CODE", "Ali", "Ahmadi", "09123456789", "   ", "ایمیل مدیر مرکز الزامی است.")]
    [InlineData("Name", "CODE", "Ali", "Ahmadi", "09123456789", "not-an-email", "فرمت ایمیل مدیر مرکز نامعتبر است.")]
    public async Task ExecuteAsync_WithMissingOrInvalidFields_ThrowsArgumentExceptionWithPersianMessage(
        string name, string code, string managerFirstName, string managerLastName, string managerMobile, string managerEmail, string expectedMessage)
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        var managerIdentityService = new OrganizationManagerIdentityService(orgDb, identityDb);
        var useCase = new CreateOrganizationUseCase(managerIdentityService);

        var request = new CreateOrganizationRequest
        {
            Name = name,
            Code = code,
            Type = OrganizationType.Clinic,
            ManagerFirstName = managerFirstName,
            ManagerLastName = managerLastName,
            ManagerMobile = managerMobile,
            ManagerEmail = managerEmail
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, Guid.NewGuid()));
        Assert.Contains(expectedMessage, ex.Message);
        Assert.Empty(orgDb.Organizations);
    }

    [Fact]
    public async Task Controller_Create_WithValidSystemAdminClaims_Returns201Created()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        var repository = new OrganizationRepository(orgDb);
        var managerIdentityService = new OrganizationManagerIdentityService(orgDb, identityDb);
        var useCase = new CreateOrganizationUseCase(managerIdentityService);
        var countUseCase = new GetTotalOrganizationsCountUseCase(repository);
        var userRepo = new UserRepository(identityDb);
        var summaryUseCase = new GetOrganizationsDashboardSummaryUseCase(userRepo);
        var recentUseCase = new GetRecentOrganizationsUseCase(repository);
        var controller = new OrganizationsController(useCase, countUseCase, summaryUseCase, recentUseCase);

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
            ManagerMobile = "09123456789",
            ManagerEmail = "ali.ahmadi@clinic.com"
        };

        // Act
        var actionResult = await controller.Create(request, CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);

        var dto = Assert.IsType<OrganizationDto>(objectResult.Value);
        Assert.Equal("SystemAdmin Org", dto.Name);
        Assert.Equal(OrganizationType.ImagingCenter, dto.OrganizationType);
        Assert.Equal(expectedUserId, dto.CreatedByUserId);

        var dbUser = await identityDb.Users.FirstAsync(u => u.PhoneNumber == "09123456789");
        Assert.Equal("ali.ahmadi@clinic.com", dbUser.Email);
    }

    [Fact]
    public async Task Controller_Create_WithUserAssignedToOtherOrg_Returns400BadRequest()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();

        identityDb.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Existing",
            LastName = "User",
            PhoneNumber = "09123456789",
            Email = "existing@clinic.com",
            OrganizationId = Guid.NewGuid()
        });
        identityDb.SaveChanges();

        var repository = new OrganizationRepository(orgDb);
        var managerIdentityService = new OrganizationManagerIdentityService(orgDb, identityDb);
        var useCase = new CreateOrganizationUseCase(managerIdentityService);
        var countUseCase = new GetTotalOrganizationsCountUseCase(repository);
        var userRepo = new UserRepository(identityDb);
        var summaryUseCase = new GetOrganizationsDashboardSummaryUseCase(userRepo);
        var recentUseCase = new GetRecentOrganizationsUseCase(repository);
        var controller = new OrganizationsController(useCase, countUseCase, summaryUseCase, recentUseCase);

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
            ManagerMobile = "09123456789",
            ManagerEmail = "ali.ahmadi@clinic.com"
        };

        // Act
        var actionResult = await controller.Create(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);

        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problemDetails.Status);
        Assert.Contains("کاربری با این شماره موبایل قبلاً به سازمان دیگری اختصاص یافته است.", problemDetails.Detail);
    }

    [Fact]
    public async Task Controller_Create_WithNullRequest_Returns400BadRequest()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        var repository = new OrganizationRepository(orgDb);
        var managerIdentityService = new OrganizationManagerIdentityService(orgDb, identityDb);
        var useCase = new CreateOrganizationUseCase(managerIdentityService);
        var countUseCase = new GetTotalOrganizationsCountUseCase(repository);
        var userRepo = new UserRepository(identityDb);
        var summaryUseCase = new GetOrganizationsDashboardSummaryUseCase(userRepo);
        var recentUseCase = new GetRecentOrganizationsUseCase(repository);
        var controller = new OrganizationsController(useCase, countUseCase, summaryUseCase, recentUseCase);

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
        var repository = new OrganizationRepository(orgDb);
        var managerIdentityService = new OrganizationManagerIdentityService(orgDb, identityDb);
        var useCase = new CreateOrganizationUseCase(managerIdentityService);
        var countUseCase = new GetTotalOrganizationsCountUseCase(repository);
        var userRepo = new UserRepository(identityDb);
        var summaryUseCase = new GetOrganizationsDashboardSummaryUseCase(userRepo);
        var recentUseCase = new GetRecentOrganizationsUseCase(repository);
        var controller = new OrganizationsController(useCase, countUseCase, summaryUseCase, recentUseCase);

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
            ManagerMobile = "09123456789",
            ManagerEmail = "ali@clinic.com"
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
        var createMethod = controllerType.GetMethod("Create");
        var authorizeAttr = createMethod?.GetCustomAttribute<AuthorizeAttribute>();

        // Assert
        Assert.NotNull(authorizeAttr);
        Assert.Equal("SystemAdmin", authorizeAttr.Roles);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCenterManagerRoleIsMissing_ThrowsInvalidOperationExceptionAndNotNullReferenceException()
    {
        // Arrange
        string dbName = Guid.NewGuid().ToString();
        var orgOptions = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        var identityOptions = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var orgDb = new OrganizationDbContext(orgOptions);
        var identityDb = new IdentityDbContext(identityOptions);

        // Do NOT seed CenterManager role
        identityDb.Roles.Add(new Role { Id = Guid.NewGuid(), Name = "SystemAdmin", Description = "System Administrator" });
        identityDb.SaveChanges();

        var managerIdentityService = new OrganizationManagerIdentityService(orgDb, identityDb);
        var useCase = new CreateOrganizationUseCase(managerIdentityService);

        var request = new CreateOrganizationRequest
        {
            Name = "No Role Center",
            Code = "NOROLE-01",
            Type = OrganizationType.Clinic,
            ManagerFirstName = "Ali",
            ManagerLastName = "Ahmadi",
            ManagerMobile = "09123456789",
            ManagerEmail = "ali@clinic.com"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(request, Guid.NewGuid()));
        Assert.Contains("Role 'CenterManager' does not exist in the database.", ex.Message);

        // Verify that no organization or user was created
        Assert.Empty(orgDb.Organizations);
        Assert.Empty(identityDb.Users);
    }
}

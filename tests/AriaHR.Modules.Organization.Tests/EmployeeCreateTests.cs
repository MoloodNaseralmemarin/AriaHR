using System.Reflection;
using System.Security.Claims;
using AriaHR.Modules.Identity.Domain.Entities;
using AriaHR.Modules.Identity.Infrastructure.Persistence;
using AriaHR.Modules.Organization.API.Controllers;
using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.UseCases.CreateEmployee;
using AriaHR.Modules.Organization.Domain.Entities;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using AriaHR.Modules.Organization.Infrastructure.Services;
using AriaHR.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AriaHR.Modules.Organization.Tests;

public class EmployeeCreateTests
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

        return (orgDb, identityDb);
    }

    private async Task SeedEmployeeRoleAsync(IdentityDbContext identityDb)
    {
        if (!await identityDb.Roles.AnyAsync(r => r.Name == "Employee"))
        {
            identityDb.Roles.Add(new Role
            {
                Id = Guid.NewGuid(),
                Name = "Employee",
                Description = "Employee Role",
                CreatedAtUtc = DateTime.UtcNow
            });
            await identityDb.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRequest_CreatesUserAndEmployeeSuccessfully()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        await SeedEmployeeRoleAsync(identityDb);

        var orgId = Guid.NewGuid();
        orgDb.Organizations.Add(new Domain.Entities.Organization
        {
            Id = orgId,
            Name = "Test Hospital",
            Code = "TH-01",
            Type = OrganizationType.Clinic,
            IsActive = true
        });
        await orgDb.SaveChangesAsync();

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var useCase = new CreateEmployeeUseCase(identityService);

        var request = new CreateEmployeeRequest
        {
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "09120001122",
            Email = "john.doe@example.com",
            PersonnelCode = "EMP-001",
            NationalCode = "1234567890",
            BirthDate = new DateOnly(1990, 5, 15),
            HireDate = new DateOnly(2022, 1, 10),
            Gender = "Male",
            ProfileImagePath = "/images/emp001.png"
        };

        var creatorId = Guid.NewGuid();

        // Act
        var result = await useCase.ExecuteAsync(request, orgId, creatorId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.NotEqual(Guid.Empty, result.UserId);
        Assert.Equal(orgId, result.OrganizationId);
        Assert.Equal("EMP-001", result.PersonnelCode);
        Assert.Equal("1234567890", result.NationalCode);
        Assert.Equal(new DateOnly(1990, 5, 15), result.BirthDate);
        Assert.Equal(new DateOnly(2022, 1, 10), result.HireDate);
        Assert.Equal("Male", result.Gender);
        Assert.True(result.IsActive);
        Assert.Equal("/images/emp001.png", result.ProfileImagePath);
        Assert.Equal(creatorId, result.CreatedByUserId);

        // Verify User saved in Identity database
        var createdUser = await identityDb.Users.FirstOrDefaultAsync(u => u.Id == result.UserId);
        Assert.NotNull(createdUser);
        Assert.Equal("John", createdUser.FirstName);
        Assert.Equal("Doe", createdUser.LastName);
        Assert.Equal("09120001122", createdUser.PhoneNumber);
        Assert.Equal("john.doe@example.com", createdUser.Email);
        Assert.Equal(orgId, createdUser.OrganizationId);

        // Verify Employee role assigned to User
        var employeeRole = await identityDb.Roles.FirstAsync(r => r.Name == "Employee");
        var userRoleAssigned = await identityDb.UserRoles.AnyAsync(ur => ur.UserId == createdUser.Id && ur.RoleId == employeeRole.Id);
        Assert.True(userRoleAssigned);

        // Verify Employee saved in Organization database
        var dbEmployee = await orgDb.Employees.FirstOrDefaultAsync(e => e.Id == result.Id);
        Assert.NotNull(dbEmployee);
        Assert.Equal(createdUser.Id, dbEmployee.UserId);
        Assert.Equal(orgId, dbEmployee.OrganizationId);
    }

    [Fact]
    public async Task ExecuteAsync_WithExistingPhoneNumber_ThrowsArgumentException()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        await SeedEmployeeRoleAsync(identityDb);

        var orgId = Guid.NewGuid();
        orgDb.Organizations.Add(new Domain.Entities.Organization
        {
            Id = orgId,
            Name = "Test Hospital",
            Code = "TH-01",
            Type = OrganizationType.Clinic,
            IsActive = true
        });
        await orgDb.SaveChangesAsync();

        identityDb.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Existing",
            LastName = "User",
            PhoneNumber = "09120001122",
            IsActive = true
        });
        await identityDb.SaveChangesAsync();

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var useCase = new CreateEmployeeUseCase(identityService);

        var request = new CreateEmployeeRequest
        {
            FirstName = "New",
            LastName = "User",
            PhoneNumber = "09120001122", // existing phone
            PersonnelCode = "EMP-001",
            NationalCode = "1234567890",
            BirthDate = new DateOnly(1990, 5, 15),
            HireDate = new DateOnly(2022, 1, 10)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, orgId, Guid.NewGuid()));
        Assert.Contains("شماره موبایل", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentOrganization_ThrowsArgumentException()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        await SeedEmployeeRoleAsync(identityDb);

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var useCase = new CreateEmployeeUseCase(identityService);

        var request = new CreateEmployeeRequest
        {
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "09120001122",
            PersonnelCode = "EMP-001",
            NationalCode = "1234567890",
            BirthDate = new DateOnly(1990, 5, 15),
            HireDate = new DateOnly(2022, 1, 10)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, Guid.NewGuid(), Guid.NewGuid()));
        Assert.Contains("سازمان مورد نظر یافت نشد", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithFutureBirthDate_ThrowsArgumentException()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        await SeedEmployeeRoleAsync(identityDb);

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var useCase = new CreateEmployeeUseCase(identityService);

        var request = new CreateEmployeeRequest
        {
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "09120001122",
            PersonnelCode = "EMP-001",
            NationalCode = "1234567890",
            BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            HireDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, Guid.NewGuid(), Guid.NewGuid()));
        Assert.Contains("تاریخ تولد باید در گذشته باشد", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithHireDateBeforeBirthDate_ThrowsArgumentException()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        await SeedEmployeeRoleAsync(identityDb);

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var useCase = new CreateEmployeeUseCase(identityService);

        var request = new CreateEmployeeRequest
        {
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "09120001122",
            PersonnelCode = "EMP-001",
            NationalCode = "1234567890",
            BirthDate = new DateOnly(2000, 1, 1),
            HireDate = new DateOnly(1999, 1, 1)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, Guid.NewGuid(), Guid.NewGuid()));
        Assert.Contains("قبل از تاریخ تولد", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithDuplicateNationalCode_ThrowsArgumentException()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        await SeedEmployeeRoleAsync(identityDb);

        var orgId = Guid.NewGuid();
        orgDb.Organizations.Add(new Domain.Entities.Organization
        {
            Id = orgId,
            Name = "Hospital",
            Code = "H1",
            Type = OrganizationType.Clinic,
            IsActive = true
        });

        orgDb.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            OrganizationId = orgId,
            PersonnelCode = "P-100",
            NationalCode = "1234567890",
            BirthDate = new DateOnly(1990, 1, 1),
            HireDate = new DateOnly(2020, 1, 1),
            IsActive = true
        });
        await orgDb.SaveChangesAsync();

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var useCase = new CreateEmployeeUseCase(identityService);

        var request = new CreateEmployeeRequest
        {
            FirstName = "Emp2",
            LastName = "User2",
            PhoneNumber = "09120005566",
            PersonnelCode = "P-200",
            NationalCode = "1234567890", // duplicate national code
            BirthDate = new DateOnly(1995, 1, 1),
            HireDate = new DateOnly(2021, 1, 1)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, orgId, Guid.NewGuid()));
        Assert.Contains("کد ملی وارد شده تکراری است", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithDuplicatePersonnelCodeInSameOrg_ThrowsArgumentException()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        await SeedEmployeeRoleAsync(identityDb);

        var orgId = Guid.NewGuid();
        orgDb.Organizations.Add(new Domain.Entities.Organization
        {
            Id = orgId,
            Name = "Hospital",
            Code = "H1",
            Type = OrganizationType.Clinic,
            IsActive = true
        });

        orgDb.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            OrganizationId = orgId,
            PersonnelCode = "P-100",
            NationalCode = "1111111111",
            BirthDate = new DateOnly(1990, 1, 1),
            HireDate = new DateOnly(2020, 1, 1),
            IsActive = true
        });
        await orgDb.SaveChangesAsync();

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var useCase = new CreateEmployeeUseCase(identityService);

        var request = new CreateEmployeeRequest
        {
            FirstName = "Emp2",
            LastName = "User2",
            PhoneNumber = "09120007788",
            PersonnelCode = "P-100", // duplicate personnel code in same org
            NationalCode = "2222222222",
            BirthDate = new DateOnly(1995, 1, 1),
            HireDate = new DateOnly(2021, 1, 1)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, orgId, Guid.NewGuid()));
        Assert.Contains("کد پرسنلی در این سازمان تکراری است", ex.Message);
    }

    [Fact]
    public async Task Controller_CreateEmployee_AsCenterManager_DerivesOrgIdFromToken()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        await SeedEmployeeRoleAsync(identityDb);

        var managerOrgId = Guid.NewGuid();
        orgDb.Organizations.Add(new Domain.Entities.Organization
        {
            Id = managerOrgId,
            Name = "Center Manager Hospital",
            Code = "CMH-01",
            Type = OrganizationType.Clinic,
            IsActive = true
        });
        await orgDb.SaveChangesAsync();

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var useCase = new CreateEmployeeUseCase(identityService);

        var currentUserId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, currentUserId.ToString()),
            new Claim(ClaimTypes.Role, "CenterManager"),
            new Claim("organization_id", managerOrgId.ToString())
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var currentUserService = new CurrentUserService(httpContextAccessor);

        var controller = new EmployeesController(useCase, currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var request = new CreateEmployeeRequest
        {
            FirstName = "Emp",
            LastName = "User",
            PhoneNumber = "09121112233",
            PersonnelCode = "P-888",
            NationalCode = "9876543210",
            BirthDate = new DateOnly(1988, 8, 8),
            HireDate = new DateOnly(2023, 3, 3),
            OrganizationId = Guid.NewGuid() // Client attempt to override OrganizationId
        };

        // Act
        var actionResult = await controller.CreateEmployee(request, CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);

        var dto = Assert.IsType<EmployeeDto>(objectResult.Value);
        Assert.Equal(managerOrgId, dto.OrganizationId); // Verifies OrganizationId was derived from token, not request payload
    }

    [Fact]
    public async Task Controller_CreateEmployee_AsCenterManagerWithoutOrgClaim_ReturnsForbid()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        await SeedEmployeeRoleAsync(identityDb);

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var useCase = new CreateEmployeeUseCase(identityService);

        var currentUserId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, currentUserId.ToString()),
            new Claim(ClaimTypes.Role, "CenterManager") // No organization_id claim
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var currentUserService = new CurrentUserService(httpContextAccessor);

        var controller = new EmployeesController(useCase, currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var request = new CreateEmployeeRequest
        {
            FirstName = "Emp",
            LastName = "User",
            PhoneNumber = "09121112233",
            PersonnelCode = "P-999",
            NationalCode = "9999999999",
            BirthDate = new DateOnly(1988, 8, 8),
            HireDate = new DateOnly(2023, 3, 3)
        };

        // Act
        var actionResult = await controller.CreateEmployee(request, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(actionResult);
    }

    [Fact]
    public async Task Controller_CreateEmployee_AsSystemAdmin_UsesRequestBodyOrgId()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        await SeedEmployeeRoleAsync(identityDb);

        var targetOrgId = Guid.NewGuid();
        orgDb.Organizations.Add(new Domain.Entities.Organization
        {
            Id = targetOrgId,
            Name = "SysAdmin Target Org",
            Code = "SATO-01",
            Type = OrganizationType.Clinic,
            IsActive = true
        });
        await orgDb.SaveChangesAsync();

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var useCase = new CreateEmployeeUseCase(identityService);

        var currentUserId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, currentUserId.ToString()),
            new Claim(ClaimTypes.Role, "SystemAdmin")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var currentUserService = new CurrentUserService(httpContextAccessor);

        var controller = new EmployeesController(useCase, currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var request = new CreateEmployeeRequest
        {
            FirstName = "SysAdminEmp",
            LastName = "User",
            PhoneNumber = "09122223344",
            PersonnelCode = "P-SYS",
            NationalCode = "5555555555",
            BirthDate = new DateOnly(1985, 5, 5),
            HireDate = new DateOnly(2020, 2, 2),
            OrganizationId = targetOrgId
        };

        // Act
        var actionResult = await controller.CreateEmployee(request, CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);

        var dto = Assert.IsType<EmployeeDto>(objectResult.Value);
        Assert.Equal(targetOrgId, dto.OrganizationId);
    }

    [Fact]
    public void Controller_HasAuthorizeAttribute_RestrictedToCenterManagerAndSystemAdmin()
    {
        // Arrange & Act
        var controllerType = typeof(EmployeesController);
        var authorizeAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        // Assert
        Assert.NotNull(authorizeAttr);
        Assert.Equal("CenterManager,SystemAdmin", authorizeAttr.Roles);
    }
}

using System.Reflection;
using System.Security.Claims;
using AriaHR.Modules.Identity.Domain.Entities;
using AriaHR.Modules.Identity.Infrastructure.Persistence;
using AriaHR.Modules.Identity.Infrastructure.Repositories;
using AriaHR.Modules.Organization.API.Controllers;
using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.UseCases.CreateEmployee;
using AriaHR.Modules.Organization.Domain.Entities;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using AriaHR.Modules.Organization.Infrastructure.Repositories;
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

    [Fact]
    public async Task ExecuteAsync_WithValidRequest_CreatesEmployeeSuccessfully()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

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
            Id = userId,
            FirstName = "Emp",
            LastName = "User",
            PhoneNumber = "09120001122",
            OrganizationId = orgId,
            IsActive = true
        });
        await identityDb.SaveChangesAsync();

        var repo = new EmployeeRepository(orgDb);
        var userRepo = new UserRepository(identityDb);
        var useCase = new CreateEmployeeUseCase(repo, userRepo);

        var request = new CreateEmployeeRequest
        {
            UserId = userId,
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
        Assert.Equal(request.UserId, result.UserId);
        Assert.Equal(orgId, result.OrganizationId);
        Assert.Equal("EMP-001", result.PersonnelCode);
        Assert.Equal("1234567890", result.NationalCode);
        Assert.Equal(new DateOnly(1990, 5, 15), result.BirthDate);
        Assert.Equal(new DateOnly(2022, 1, 10), result.HireDate);
        Assert.Equal("Male", result.Gender);
        Assert.True(result.IsActive);
        Assert.Equal("/images/emp001.png", result.ProfileImagePath);
        Assert.Equal(creatorId, result.CreatedByUserId);

        var dbEmployee = await orgDb.Employees.FirstOrDefaultAsync(e => e.Id == result.Id);
        Assert.NotNull(dbEmployee);
        Assert.Equal(orgId, dbEmployee.OrganizationId);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentUser_ThrowsArgumentException()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        var repo = new EmployeeRepository(orgDb);
        var userRepo = new UserRepository(identityDb);
        var useCase = new CreateEmployeeUseCase(repo, userRepo);

        var request = new CreateEmployeeRequest
        {
            UserId = Guid.NewGuid(),
            PersonnelCode = "EMP-001",
            NationalCode = "1234567890",
            BirthDate = new DateOnly(1990, 5, 15),
            HireDate = new DateOnly(2022, 1, 10)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, Guid.NewGuid(), Guid.NewGuid()));
        Assert.Contains("کاربر مورد نظر یافت نشد", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithUserBelongingToDifferentOrg_ThrowsArgumentException()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        var targetOrgId = Guid.NewGuid();
        var userOrgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        identityDb.Users.Add(new User
        {
            Id = userId,
            FirstName = "OtherOrg",
            LastName = "User",
            PhoneNumber = "09120003344",
            OrganizationId = userOrgId,
            IsActive = true
        });
        await identityDb.SaveChangesAsync();

        var repo = new EmployeeRepository(orgDb);
        var userRepo = new UserRepository(identityDb);
        var useCase = new CreateEmployeeUseCase(repo, userRepo);

        var request = new CreateEmployeeRequest
        {
            UserId = userId,
            PersonnelCode = "EMP-001",
            NationalCode = "1234567890",
            BirthDate = new DateOnly(1990, 5, 15),
            HireDate = new DateOnly(2022, 1, 10)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, targetOrgId, Guid.NewGuid()));
        Assert.Contains("کاربر به سازمان دیگری تعلق دارد", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyUserId_ThrowsArgumentException()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        var repo = new EmployeeRepository(orgDb);
        var userRepo = new UserRepository(identityDb);
        var useCase = new CreateEmployeeUseCase(repo, userRepo);

        var request = new CreateEmployeeRequest
        {
            UserId = Guid.Empty,
            PersonnelCode = "EMP-001",
            NationalCode = "1234567890",
            BirthDate = new DateOnly(1990, 5, 15),
            HireDate = new DateOnly(2022, 1, 10)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, Guid.NewGuid(), Guid.NewGuid()));
        Assert.Contains("شناسه کاربر الزامی است", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithFutureBirthDate_ThrowsArgumentException()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        var repo = new EmployeeRepository(orgDb);
        var userRepo = new UserRepository(identityDb);
        var useCase = new CreateEmployeeUseCase(repo, userRepo);

        var request = new CreateEmployeeRequest
        {
            UserId = Guid.NewGuid(),
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
        var repo = new EmployeeRepository(orgDb);
        var userRepo = new UserRepository(identityDb);
        var useCase = new CreateEmployeeUseCase(repo, userRepo);

        var request = new CreateEmployeeRequest
        {
            UserId = Guid.NewGuid(),
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
        var orgId = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

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
            UserId = userId1,
            OrganizationId = orgId,
            PersonnelCode = "P-100",
            NationalCode = "1234567890",
            BirthDate = new DateOnly(1990, 1, 1),
            HireDate = new DateOnly(2020, 1, 1),
            IsActive = true
        });
        await orgDb.SaveChangesAsync();

        identityDb.Users.Add(new User
        {
            Id = userId2,
            FirstName = "Emp2",
            LastName = "User2",
            PhoneNumber = "09120005566",
            OrganizationId = orgId,
            IsActive = true
        });
        await identityDb.SaveChangesAsync();

        var repo = new EmployeeRepository(orgDb);
        var userRepo = new UserRepository(identityDb);
        var useCase = new CreateEmployeeUseCase(repo, userRepo);

        var request = new CreateEmployeeRequest
        {
            UserId = userId2,
            PersonnelCode = "P-200",
            NationalCode = "1234567890", // duplicate
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
        var orgId = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

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
            UserId = userId1,
            OrganizationId = orgId,
            PersonnelCode = "P-100",
            NationalCode = "1111111111",
            BirthDate = new DateOnly(1990, 1, 1),
            HireDate = new DateOnly(2020, 1, 1),
            IsActive = true
        });
        await orgDb.SaveChangesAsync();

        identityDb.Users.Add(new User
        {
            Id = userId2,
            FirstName = "Emp2",
            LastName = "User2",
            PhoneNumber = "09120007788",
            OrganizationId = orgId,
            IsActive = true
        });
        await identityDb.SaveChangesAsync();

        var repo = new EmployeeRepository(orgDb);
        var userRepo = new UserRepository(identityDb);
        var useCase = new CreateEmployeeUseCase(repo, userRepo);

        var request = new CreateEmployeeRequest
        {
            UserId = userId2,
            PersonnelCode = "P-100", // duplicate in same org
            NationalCode = "2222222222",
            BirthDate = new DateOnly(1995, 1, 1),
            HireDate = new DateOnly(2021, 1, 1)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, orgId, Guid.NewGuid()));
        Assert.Contains("کد پرسنلی در این سازمان تکراری است", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithDuplicateUserId_ThrowsArgumentException()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

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
            UserId = userId,
            OrganizationId = orgId,
            PersonnelCode = "P-100",
            NationalCode = "1111111111",
            BirthDate = new DateOnly(1990, 1, 1),
            HireDate = new DateOnly(2020, 1, 1),
            IsActive = true
        });
        await orgDb.SaveChangesAsync();

        identityDb.Users.Add(new User
        {
            Id = userId,
            FirstName = "Emp",
            LastName = "User",
            PhoneNumber = "09120009900",
            OrganizationId = orgId,
            IsActive = true
        });
        await identityDb.SaveChangesAsync();

        var repo = new EmployeeRepository(orgDb);
        var userRepo = new UserRepository(identityDb);
        var useCase = new CreateEmployeeUseCase(repo, userRepo);

        var request = new CreateEmployeeRequest
        {
            UserId = userId, // duplicate user
            PersonnelCode = "P-200",
            NationalCode = "2222222222",
            BirthDate = new DateOnly(1995, 1, 1),
            HireDate = new DateOnly(2021, 1, 1)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, orgId, Guid.NewGuid()));
        Assert.Contains("قبلاً به عنوان کارمند ثبت شده است", ex.Message);
    }

    [Fact]
    public async Task Controller_CreateEmployee_AsCenterManager_DerivesOrgIdFromToken()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        var managerOrgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        orgDb.Organizations.Add(new Domain.Entities.Organization
        {
            Id = managerOrgId,
            Name = "Center Manager Hospital",
            Code = "CMH-01",
            Type = OrganizationType.Clinic,
            IsActive = true
        });
        await orgDb.SaveChangesAsync();

        identityDb.Users.Add(new User
        {
            Id = userId,
            FirstName = "Emp",
            LastName = "User",
            PhoneNumber = "09121112233",
            OrganizationId = managerOrgId,
            IsActive = true
        });
        await identityDb.SaveChangesAsync();

        var repo = new EmployeeRepository(orgDb);
        var userRepo = new UserRepository(identityDb);
        var useCase = new CreateEmployeeUseCase(repo, userRepo);

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
            UserId = userId,
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
        var repo = new EmployeeRepository(orgDb);
        var userRepo = new UserRepository(identityDb);
        var useCase = new CreateEmployeeUseCase(repo, userRepo);

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
            UserId = Guid.NewGuid(),
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
        var targetOrgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        orgDb.Organizations.Add(new Domain.Entities.Organization
        {
            Id = targetOrgId,
            Name = "SysAdmin Target Org",
            Code = "SATO-01",
            Type = OrganizationType.Clinic,
            IsActive = true
        });
        await orgDb.SaveChangesAsync();

        identityDb.Users.Add(new User
        {
            Id = userId,
            FirstName = "SysAdminEmp",
            LastName = "User",
            PhoneNumber = "09122223344",
            OrganizationId = targetOrgId,
            IsActive = true
        });
        await identityDb.SaveChangesAsync();

        var repo = new EmployeeRepository(orgDb);
        var userRepo = new UserRepository(identityDb);
        var useCase = new CreateEmployeeUseCase(repo, userRepo);

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
            UserId = userId,
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

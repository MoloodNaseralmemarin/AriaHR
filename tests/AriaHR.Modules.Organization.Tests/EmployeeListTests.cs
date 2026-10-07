using System.Security.Claims;
using AriaHR.Modules.Identity.Domain.Entities;
using AriaHR.Modules.Identity.Infrastructure.Persistence;
using AriaHR.Modules.Organization.API.Controllers;
using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.UseCases.CreateEmployee;
using AriaHR.Modules.Organization.Application.UseCases.GetEmployees;
using AriaHR.Modules.Organization.Domain.Entities;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using AriaHR.Modules.Organization.Infrastructure.Services;
using AriaHR.Shared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AriaHR.Modules.Organization.Tests;

public class EmployeeListTests
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

    private class FakeCreateEmployeeUseCase : ICreateEmployeeUseCase
    {
        public Task<EmployeeDto> ExecuteAsync(CreateEmployeeRequest request, Guid organizationId, Guid createdByUserId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new EmployeeDto { Id = Guid.NewGuid(), OrganizationId = organizationId });
        }
    }

    [Fact]
    public async Task GetEmployeesByOrganizationAsync_ReturnsEmployeesWithMappedUserDetails()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        var orgId = Guid.NewGuid();

        var user1 = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Ali",
            LastName = "Rezai",
            PhoneNumber = "09121111111",
            Email = "ali@example.com",
            OrganizationId = orgId,
            IsActive = true
        };

        var user2 = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Sara",
            LastName = "Ahmadi",
            PhoneNumber = "09122222222",
            Email = "sara@example.com",
            OrganizationId = orgId,
            IsActive = true
        };

        identityDb.Users.AddRange(user1, user2);
        await identityDb.SaveChangesAsync();

        var emp1 = new Employee
        {
            Id = Guid.NewGuid(),
            UserId = user1.Id,
            OrganizationId = orgId,
            PersonnelCode = "P-001",
            NationalCode = "1000000001",
            BirthDate = new DateOnly(1990, 1, 1),
            HireDate = new DateOnly(2020, 1, 1),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10)
        };

        var emp2 = new Employee
        {
            Id = Guid.NewGuid(),
            UserId = user2.Id,
            OrganizationId = orgId,
            PersonnelCode = "P-002",
            NationalCode = "1000000002",
            BirthDate = new DateOnly(1992, 2, 2),
            HireDate = new DateOnly(2021, 2, 2),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        orgDb.Employees.AddRange(emp1, emp2);
        await orgDb.SaveChangesAsync();

        var identityService = new EmployeeIdentityService(orgDb, identityDb);

        // Act
        var result = (await identityService.GetEmployeesByOrganizationAsync(orgId)).ToList();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        var firstMapped = result.First(e => e.Id == emp2.Id);
        Assert.Equal("Sara", firstMapped.FirstName);
        Assert.Equal("Ahmadi", firstMapped.LastName);
        Assert.Equal("09122222222", firstMapped.PhoneNumber);
        Assert.Equal("sara@example.com", firstMapped.Email);
        Assert.Equal("P-002", firstMapped.PersonnelCode);

        var secondMapped = result.First(e => e.Id == emp1.Id);
        Assert.Equal("Ali", secondMapped.FirstName);
        Assert.Equal("Rezai", secondMapped.LastName);
        Assert.Equal("09121111111", secondMapped.PhoneNumber);
        Assert.Equal("ali@example.com", secondMapped.Email);
        Assert.Equal("P-001", secondMapped.PersonnelCode);
    }

    [Fact]
    public async Task GetEmployeesByOrganizationAsync_WhenNoEmployeesExist_ReturnsEmptyList()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        var identityService = new EmployeeIdentityService(orgDb, identityDb);

        // Act
        var result = await identityService.GetEmployeesByOrganizationAsync(Guid.NewGuid());

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetEmployeesUseCase_WithEmptyOrgId_ThrowsArgumentException()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var useCase = new GetEmployeesUseCase(identityService);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(Guid.Empty));
    }

    [Fact]
    public async Task Controller_GetEmployees_AsCenterManager_ReturnsOwnOrganizationEmployees()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        var orgId = Guid.NewGuid();

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var getEmployeesUseCase = new GetEmployeesUseCase(identityService);
        var createEmployeeUseCase = new FakeCreateEmployeeUseCase();

        var currentUserId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, currentUserId.ToString()),
            new Claim(ClaimTypes.Role, "CenterManager"),
            new Claim("organization_id", orgId.ToString())
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var currentUserService = new CurrentUserService(httpContextAccessor);

        var controller = new EmployeesController(createEmployeeUseCase, getEmployeesUseCase, identityService, currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        // Act
        var actionResult = await controller.GetEmployees(null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var dtos = Assert.IsAssignableFrom<IEnumerable<EmployeeDto>>(okResult.Value);
        Assert.Empty(dtos);
    }

    [Fact]
    public async Task Controller_GetEmployees_AsCenterManagerAccessingOtherOrg_ReturnsForbid()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        var myOrgId = Guid.NewGuid();
        var otherOrgId = Guid.NewGuid();

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var getEmployeesUseCase = new GetEmployeesUseCase(identityService);
        var createEmployeeUseCase = new FakeCreateEmployeeUseCase();

        var currentUserId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, currentUserId.ToString()),
            new Claim(ClaimTypes.Role, "CenterManager"),
            new Claim("organization_id", myOrgId.ToString())
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var currentUserService = new CurrentUserService(httpContextAccessor);

        var controller = new EmployeesController(createEmployeeUseCase, getEmployeesUseCase, identityService, currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        // Act
        var actionResult = await controller.GetEmployees(otherOrgId, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(actionResult);
    }

    [Fact]
    public async Task Controller_GetEmployees_AsCenterManagerWithoutOrgClaim_ReturnsForbid()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var getEmployeesUseCase = new GetEmployeesUseCase(identityService);
        var createEmployeeUseCase = new FakeCreateEmployeeUseCase();

        var currentUserId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, currentUserId.ToString()),
            new Claim(ClaimTypes.Role, "CenterManager")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var currentUserService = new CurrentUserService(httpContextAccessor);

        var controller = new EmployeesController(createEmployeeUseCase, getEmployeesUseCase, identityService, currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        // Act
        var actionResult = await controller.GetEmployees(null, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(actionResult);
    }

    [Fact]
    public async Task Controller_GetEmployees_AsSystemAdminWithOrgQueryParam_ReturnsRequestedOrgEmployees()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        var requestedOrgId = Guid.NewGuid();

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var getEmployeesUseCase = new GetEmployeesUseCase(identityService);
        var createEmployeeUseCase = new FakeCreateEmployeeUseCase();

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

        var controller = new EmployeesController(createEmployeeUseCase, getEmployeesUseCase, identityService, currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        // Act
        var actionResult = await controller.GetEmployees(requestedOrgId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Controller_GetEmployees_AsSystemAdminWithoutOrgParamOrClaim_ReturnsBadRequest()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var getEmployeesUseCase = new GetEmployeesUseCase(identityService);
        var createEmployeeUseCase = new FakeCreateEmployeeUseCase();

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

        var controller = new EmployeesController(createEmployeeUseCase, getEmployeesUseCase, identityService, currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        // Act
        var actionResult = await controller.GetEmployees(null, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        var problem = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
    }
}

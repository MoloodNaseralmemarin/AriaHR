using System.Security.Claims;
using AriaHR.Modules.Organization.API.Controllers;
using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.UseCases.CreateOrganization;
using AriaHR.Modules.Organization.Application.UseCases.GetDashboardSummary;
using AriaHR.Modules.Organization.Application.UseCases.GetOrganizationCenters;
using AriaHR.Modules.Organization.Application.UseCases.GetOrganizations;
using AriaHR.Modules.Organization.Application.UseCases.GetRecentOrganizations;
using AriaHR.Modules.Organization.Application.UseCases.GetTotalOrganizationsCount;
using AriaHR.Modules.Organization.Application.UseCases.UpdateCenter;
using AriaHR.Modules.Organization.Application.UseCases.UpdateCenterStatus;
using AriaHR.Modules.Organization.Domain.Entities;
using AriaHR.Modules.Organization.Domain.Enums;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using AriaHR.Modules.Organization.Infrastructure.Repositories;
using AriaHR.Shared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AriaHR.Modules.Organization.Tests;

public class OrganizationListAndCenterManagementTests
{
    private OrganizationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new OrganizationDbContext(options);
    }

    private class FakeCreateOrganizationUseCase : ICreateOrganizationUseCase
    {
        public Task<OrganizationDto> ExecuteAsync(CreateOrganizationRequest request, Guid createdByUserId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new OrganizationDto { Id = Guid.NewGuid() });
        }
    }

    private class FakeGetTotalOrganizationsCountUseCase : IGetTotalOrganizationsCountUseCase
    {
        public Task<OrganizationCountResponse> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new OrganizationCountResponse { TotalCount = 1 });
        }
    }

    private class FakeGetOrganizationsDashboardSummaryUseCase : IGetOrganizationsDashboardSummaryUseCase
    {
        public Task<OrganizationsDashboardSummaryResponse> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new OrganizationsDashboardSummaryResponse());
        }
    }

    private class FakeGetRecentOrganizationsUseCase : IGetRecentOrganizationsUseCase
    {
        public Task<IReadOnlyList<RecentOrganizationDto>> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<RecentOrganizationDto>>(new List<RecentOrganizationDto>());
        }
    }

    #region 1. Organization List Tests

    [Fact]
    public async Task GetOrganizationsUseCase_ReturnsNonDeletedOrganizationsOnly()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var org1 = new Domain.Entities.Organization
        {
            Id = Guid.NewGuid(),
            Name = "Clinic Alpha",
            Code = "ORG001",
            OrganizationType = OrganizationType.Clinic,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        var org2 = new Domain.Entities.Organization
        {
            Id = Guid.NewGuid(),
            Name = "Lab Beta",
            Code = "ORG002",
            OrganizationType = OrganizationType.Laboratory,
            IsActive = false,
            CreatedAtUtc = DateTime.UtcNow
        };
        var deletedOrg = new Domain.Entities.Organization
        {
            Id = Guid.NewGuid(),
            Name = "Deleted Center",
            Code = "ORG003",
            OrganizationType = OrganizationType.Pharmacy,
            IsActive = true,
            IsDeleted = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.Organizations.AddRange(org1, org2, deletedOrg);
        await db.SaveChangesAsync();

        var repo = new OrganizationRepository(db);
        var useCase = new GetOrganizationsUseCase(repo);

        // Act
        var result = (await useCase.ExecuteAsync()).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, o => o.Id == org1.Id && o.Name == "Clinic Alpha" && o.OrganizationType == OrganizationType.Clinic && o.IsActive);
        Assert.Contains(result, o => o.Id == org2.Id && o.Name == "Lab Beta" && o.OrganizationType == OrganizationType.Laboratory && !o.IsActive);
        Assert.DoesNotContain(result, o => o.Id == deletedOrg.Id);
    }

    [Fact]
    public async Task OrganizationsController_GetOrganizations_ReturnsOkWithList()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var repo = new OrganizationRepository(db);
        var getOrgsUseCase = new GetOrganizationsUseCase(repo);

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

        var controller = new OrganizationsController(
            new FakeCreateOrganizationUseCase(),
            new FakeGetTotalOrganizationsCountUseCase(),
            new FakeGetOrganizationsDashboardSummaryUseCase(),
            new FakeGetRecentOrganizationsUseCase(),
            getOrgsUseCase,
            new GetOrganizationCentersUseCase(repo, new BranchRepository(db)),
            currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        // Act
        var actionResult = await controller.GetOrganizations(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var items = Assert.IsAssignableFrom<IEnumerable<OrganizationListItemDto>>(okResult.Value);
        Assert.Empty(items);
    }

    #endregion

    #region 2. Get Organization Centers Tests

    [Fact]
    public async Task GetOrganizationCentersUseCase_WithValidOrgId_ReturnsBranchesForOrg()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var otherOrgId = Guid.NewGuid();

        var org = new Domain.Entities.Organization
        {
            Id = orgId,
            Name = "Main Org",
            Code = "MO01",
            OrganizationType = OrganizationType.Clinic,
            IsActive = true
        };
        db.Organizations.Add(org);

        var branch1 = new Branch
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Name = "Branch 1",
            Code = "B01",
            Address = "Address 1",
            Phone = "02111111111",
            IsActive = true
        };
        var branch2 = new Branch
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Name = "Branch 2",
            Code = "B02",
            Address = "Address 2",
            Phone = "02122222222",
            IsActive = false
        };
        var otherBranch = new Branch
        {
            Id = Guid.NewGuid(),
            OrganizationId = otherOrgId,
            Name = "Other Branch",
            Code = "OB01",
            IsActive = true
        };
        var deletedBranch = new Branch
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Name = "Deleted Branch",
            Code = "DB01",
            IsDeleted = true,
            IsActive = true
        };

        db.Branches.AddRange(branch1, branch2, otherBranch, deletedBranch);
        await db.SaveChangesAsync();

        var orgRepo = new OrganizationRepository(db);
        var branchRepo = new BranchRepository(db);
        var useCase = new GetOrganizationCentersUseCase(orgRepo, branchRepo);

        // Act
        var result = (await useCase.ExecuteAsync(orgId, isSystemAdmin: true, userOrganizationId: null)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, b => b.Id == branch1.Id && b.Name == "Branch 1");
        Assert.Contains(result, b => b.Id == branch2.Id && b.Name == "Branch 2");
        Assert.DoesNotContain(result, b => b.Id == otherBranch.Id);
        Assert.DoesNotContain(result, b => b.Id == deletedBranch.Id);
    }

    [Fact]
    public async Task GetOrganizationCentersUseCase_WhenOrgNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var orgRepo = new OrganizationRepository(db);
        var branchRepo = new BranchRepository(db);
        var useCase = new GetOrganizationCentersUseCase(orgRepo, branchRepo);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            useCase.ExecuteAsync(Guid.NewGuid(), isSystemAdmin: true, userOrganizationId: null));
    }

    [Fact]
    public async Task GetOrganizationCentersUseCase_AsCenterManagerAccessingOtherOrg_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var myOrgId = Guid.NewGuid();
        var targetOrgId = Guid.NewGuid();

        var orgRepo = new OrganizationRepository(db);
        var branchRepo = new BranchRepository(db);
        var useCase = new GetOrganizationCentersUseCase(orgRepo, branchRepo);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            useCase.ExecuteAsync(targetOrgId, isSystemAdmin: false, userOrganizationId: myOrgId));
    }

    [Fact]
    public async Task OrganizationsController_GetOrganizationCenters_WhenForbidden_ReturnsForbid()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var myOrgId = Guid.NewGuid();
        var otherOrgId = Guid.NewGuid();

        var orgRepo = new OrganizationRepository(db);
        var branchRepo = new BranchRepository(db);
        var getOrgsUseCase = new GetOrganizationsUseCase(orgRepo);
        var getCentersUseCase = new GetOrganizationCentersUseCase(orgRepo, branchRepo);

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

        var controller = new OrganizationsController(
            new FakeCreateOrganizationUseCase(),
            new FakeGetTotalOrganizationsCountUseCase(),
            new FakeGetOrganizationsDashboardSummaryUseCase(),
            new FakeGetRecentOrganizationsUseCase(),
            getOrgsUseCase,
            getCentersUseCase,
            currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        // Act
        var actionResult = await controller.GetOrganizationCenters(otherOrgId, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(actionResult);
    }

    [Fact]
    public async Task OrganizationsController_GetOrganizationCenters_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var orgRepo = new OrganizationRepository(db);
        var branchRepo = new BranchRepository(db);
        var getOrgsUseCase = new GetOrganizationsUseCase(orgRepo);
        var getCentersUseCase = new GetOrganizationCentersUseCase(orgRepo, branchRepo);

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

        var controller = new OrganizationsController(
            new FakeCreateOrganizationUseCase(),
            new FakeGetTotalOrganizationsCountUseCase(),
            new FakeGetOrganizationsDashboardSummaryUseCase(),
            new FakeGetRecentOrganizationsUseCase(),
            getOrgsUseCase,
            getCentersUseCase,
            currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        // Act
        var actionResult = await controller.GetOrganizationCenters(Guid.NewGuid(), CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        var problem = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
    }

    #endregion

    #region 3. Edit Center Tests

    [Fact]
    public async Task UpdateCenterUseCase_WithValidData_UpdatesBranch()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var centerId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var branch = new Branch
        {
            Id = centerId,
            OrganizationId = orgId,
            Name = "Old Center Name",
            Code = "OLD01",
            Address = "Old Address",
            Phone = "02100000000",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
        };
        db.Branches.Add(branch);
        await db.SaveChangesAsync();

        var branchRepo = new BranchRepository(db);
        var useCase = new UpdateCenterUseCase(branchRepo);

        var request = new UpdateBranchRequestDto
        {
            Name = "New Center Name",
            Code = "NEW01",
            Address = "New Address",
            Phone = "02199999999"
        };

        // Act
        var result = await useCase.ExecuteAsync(
            centerId,
            request,
            currentUserId: userId,
            isSystemAdmin: true,
            userOrganizationId: null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(centerId, result.Id);
        Assert.Equal("New Center Name", result.Name);
        Assert.Equal("NEW01", result.Code);
        Assert.Equal("New Address", result.Address);
        Assert.Equal("02199999999", result.Phone);
        Assert.True(result.IsActive);

        var updatedBranch = await db.Branches.FindAsync(centerId);
        Assert.NotNull(updatedBranch);
        Assert.Equal("New Center Name", updatedBranch.Name);
        Assert.Equal("NEW01", updatedBranch.Code);
        Assert.Equal("New Address", updatedBranch.Address);
        Assert.Equal("02199999999", updatedBranch.Phone);
        Assert.Equal(userId, updatedBranch.UpdatedByUserId);
        Assert.NotNull(updatedBranch.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateCenterUseCase_WithEmptyNameOrCode_ThrowsArgumentException()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var branchRepo = new BranchRepository(db);
        var useCase = new UpdateCenterUseCase(branchRepo);

        var requestWithoutName = new UpdateBranchRequestDto { Name = "", Code = "C01" };
        var requestWithoutCode = new UpdateBranchRequestDto { Name = "Valid Name", Code = "  " };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ExecuteAsync(Guid.NewGuid(), requestWithoutName, Guid.NewGuid(), isSystemAdmin: true, userOrganizationId: null));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ExecuteAsync(Guid.NewGuid(), requestWithoutCode, Guid.NewGuid(), isSystemAdmin: true, userOrganizationId: null));
    }

    [Fact]
    public async Task UpdateCenterUseCase_WhenCenterNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var branchRepo = new BranchRepository(db);
        var useCase = new UpdateCenterUseCase(branchRepo);

        var request = new UpdateBranchRequestDto { Name = "Name", Code = "C01" };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            useCase.ExecuteAsync(Guid.NewGuid(), request, Guid.NewGuid(), isSystemAdmin: true, userOrganizationId: null));
    }

    [Fact]
    public async Task UpdateCenterUseCase_AsCenterManagerAccessingOtherOrg_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var myOrgId = Guid.NewGuid();
        var centerId = Guid.NewGuid();

        var branch = new Branch
        {
            Id = centerId,
            OrganizationId = orgId,
            Name = "Center Name",
            Code = "C01"
        };
        db.Branches.Add(branch);
        await db.SaveChangesAsync();

        var branchRepo = new BranchRepository(db);
        var useCase = new UpdateCenterUseCase(branchRepo);

        var request = new UpdateBranchRequestDto { Name = "Updated Name", Code = "C01" };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            useCase.ExecuteAsync(centerId, request, Guid.NewGuid(), isSystemAdmin: false, userOrganizationId: myOrgId));
    }

    [Fact]
    public async Task CentersController_UpdateCenter_ReturnsOk()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var centerId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var branch = new Branch
        {
            Id = centerId,
            OrganizationId = orgId,
            Name = "Initial Name",
            Code = "C01",
            IsActive = true
        };
        db.Branches.Add(branch);
        await db.SaveChangesAsync();

        var branchRepo = new BranchRepository(db);
        var updateCenterUseCase = new UpdateCenterUseCase(branchRepo);
        var updateStatusUseCase = new UpdateCenterStatusUseCase(branchRepo);

        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "CenterManager"),
            new Claim("organization_id", orgId.ToString())
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var currentUserService = new CurrentUserService(httpContextAccessor);

        var controller = new CentersController(updateCenterUseCase, updateStatusUseCase, currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var request = new UpdateBranchRequestDto
        {
            Name = "Controller Updated Name",
            Code = "C01-UPD",
            Address = "Street 10",
            Phone = "02133333333"
        };

        // Act
        var actionResult = await controller.UpdateCenter(centerId, request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var dto = Assert.IsType<BranchListItemDto>(okResult.Value);
        Assert.Equal("Controller Updated Name", dto.Name);
        Assert.Equal("C01-UPD", dto.Code);
    }

    #endregion

    #region 4. Activate / Deactivate Center Tests

    [Fact]
    public async Task UpdateCenterStatusUseCase_Deactivate_SetsIsActiveFalseAndPreservesNotDeleted()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var centerId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var branch = new Branch
        {
            Id = centerId,
            OrganizationId = orgId,
            Name = "Active Center",
            Code = "AC01",
            IsActive = true,
            IsDeleted = false
        };
        db.Branches.Add(branch);
        await db.SaveChangesAsync();

        var branchRepo = new BranchRepository(db);
        var useCase = new UpdateCenterStatusUseCase(branchRepo);

        var request = new CenterStatusUpdateRequestDto { IsActive = false };

        // Act
        var result = await useCase.ExecuteAsync(
            centerId,
            request,
            currentUserId: userId,
            isSystemAdmin: true,
            userOrganizationId: null);

        // Assert
        Assert.False(result.IsActive);

        var updatedBranch = await db.Branches.FindAsync(centerId);
        Assert.NotNull(updatedBranch);
        Assert.False(updatedBranch.IsActive);
        Assert.False(updatedBranch.IsDeleted);
        Assert.Equal(userId, updatedBranch.UpdatedByUserId);
        Assert.NotNull(updatedBranch.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateCenterStatusUseCase_Activate_SetsIsActiveTrue()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var centerId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var branch = new Branch
        {
            Id = centerId,
            OrganizationId = orgId,
            Name = "Inactive Center",
            Code = "IC01",
            IsActive = false
        };
        db.Branches.Add(branch);
        await db.SaveChangesAsync();

        var branchRepo = new BranchRepository(db);
        var useCase = new UpdateCenterStatusUseCase(branchRepo);

        var request = new CenterStatusUpdateRequestDto { IsActive = true };

        // Act
        var result = await useCase.ExecuteAsync(
            centerId,
            request,
            currentUserId: userId,
            isSystemAdmin: true,
            userOrganizationId: null);

        // Assert
        Assert.True(result.IsActive);

        var updatedBranch = await db.Branches.FindAsync(centerId);
        Assert.NotNull(updatedBranch);
        Assert.True(updatedBranch.IsActive);
    }

    [Fact]
    public async Task UpdateCenterStatusUseCase_WhenCenterNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var branchRepo = new BranchRepository(db);
        var useCase = new UpdateCenterStatusUseCase(branchRepo);

        var request = new CenterStatusUpdateRequestDto { IsActive = true };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            useCase.ExecuteAsync(Guid.NewGuid(), request, Guid.NewGuid(), isSystemAdmin: true, userOrganizationId: null));
    }

    [Fact]
    public async Task UpdateCenterStatusUseCase_AsCenterManagerAccessingOtherOrg_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var myOrgId = Guid.NewGuid();
        var centerId = Guid.NewGuid();

        var branch = new Branch
        {
            Id = centerId,
            OrganizationId = orgId,
            Name = "Center",
            Code = "C01",
            IsActive = true
        };
        db.Branches.Add(branch);
        await db.SaveChangesAsync();

        var branchRepo = new BranchRepository(db);
        var useCase = new UpdateCenterStatusUseCase(branchRepo);

        var request = new CenterStatusUpdateRequestDto { IsActive = false };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            useCase.ExecuteAsync(centerId, request, Guid.NewGuid(), isSystemAdmin: false, userOrganizationId: myOrgId));
    }

    [Fact]
    public async Task CentersController_UpdateCenterStatus_ReturnsOk()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var centerId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var branch = new Branch
        {
            Id = centerId,
            OrganizationId = orgId,
            Name = "Center Name",
            Code = "C01",
            IsActive = true
        };
        db.Branches.Add(branch);
        await db.SaveChangesAsync();

        var branchRepo = new BranchRepository(db);
        var updateCenterUseCase = new UpdateCenterUseCase(branchRepo);
        var updateStatusUseCase = new UpdateCenterStatusUseCase(branchRepo);

        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "CenterManager"),
            new Claim("organization_id", orgId.ToString())
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var currentUserService = new CurrentUserService(httpContextAccessor);

        var controller = new CentersController(updateCenterUseCase, updateStatusUseCase, currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var request = new CenterStatusUpdateRequestDto { IsActive = false };

        // Act
        var actionResult = await controller.UpdateCenterStatus(centerId, request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var dto = Assert.IsType<BranchListItemDto>(okResult.Value);
        Assert.False(dto.IsActive);
    }

    #endregion
}

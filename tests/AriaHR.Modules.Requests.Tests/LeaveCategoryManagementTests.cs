using System.Security.Claims;
using AriaHR.Modules.Requests.API.Controllers;
using AriaHR.Modules.Requests.Application.DTOs;
using AriaHR.Modules.Requests.Application.UseCases.ActivateLeaveCategory;
using AriaHR.Modules.Requests.Application.UseCases.CreateLeaveCategory;
using AriaHR.Modules.Requests.Application.UseCases.DeactivateLeaveCategory;
using AriaHR.Modules.Requests.Application.UseCases.GetLeaveCategories;
using AriaHR.Modules.Requests.Application.UseCases.GetLeaveCategoryById;
using AriaHR.Modules.Requests.Application.UseCases.UpdateLeaveCategory;
using AriaHR.Modules.Requests.Domain.Entities;
using AriaHR.Modules.Requests.Infrastructure.Persistence;
using AriaHR.Modules.Requests.Infrastructure.Repositories;
using AriaHR.Shared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AriaHR.Modules.Requests.Tests;

public class LeaveCategoryManagementTests
{
    private RequestsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<RequestsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new RequestsDbContext(options);
    }

    [Fact]
    public async Task CreateLeaveCategory_ValidInput_CreatesCategoryWithActiveStatus()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveCategoryRepository(dbContext);
        var useCase = new CreateLeaveCategoryUseCase(repository);

        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new CreateLeaveCategoryRequest
        {
            Name = "مرخصی استحقاقی",
            MaxDaysPerYear = 30,
            IsPaid = true,
            RequiresAttachment = false
        };

        var result = await useCase.ExecuteAsync(request, orgId, userId);

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(orgId, result.OrganizationId);
        Assert.Equal("مرخصی استحقاقی", result.Name);
        Assert.Equal(30, result.MaxDaysPerYear);
        Assert.True(result.IsPaid);
        Assert.False(result.RequiresAttachment);
        Assert.True(result.IsActive);

        var dbCategory = await dbContext.LeaveCategories.FindAsync(result.Id);
        Assert.NotNull(dbCategory);
        Assert.True(dbCategory.IsActive);
    }

    [Fact]
    public async Task CreateLeaveCategory_NullMaxDaysPerYear_CreatesCategoryWithNullMaxDays()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveCategoryRepository(dbContext);
        var useCase = new CreateLeaveCategoryUseCase(repository);

        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new CreateLeaveCategoryRequest
        {
            Name = "مرخصی بدون محدودیت",
            MaxDaysPerYear = null,
            IsPaid = true,
            RequiresAttachment = false
        };

        var result = await useCase.ExecuteAsync(request, orgId, userId);

        Assert.NotNull(result);
        Assert.Null(result.MaxDaysPerYear);

        var dbCategory = await dbContext.LeaveCategories.FindAsync(result.Id);
        Assert.NotNull(dbCategory);
        Assert.Null(dbCategory.MaxDaysPerYear);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public async Task CreateLeaveCategory_ZeroOrNegativeMaxDaysPerYear_ThrowsArgumentException(int invalidMaxDays)
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveCategoryRepository(dbContext);
        var useCase = new CreateLeaveCategoryUseCase(repository);

        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new CreateLeaveCategoryRequest
        {
            Name = "مرخصی نامعتبر",
            MaxDaysPerYear = invalidMaxDays
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, orgId, userId));
        Assert.Equal("حداکثر روز مرخصی در سال باید بزرگتر از صفر باشد. (Parameter 'MaxDaysPerYear')", ex.Message);
    }

    [Fact]
    public async Task UpdateLeaveCategory_NullMaxDaysPerYear_UpdatesCategoryToNullMaxDays()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveCategoryRepository(dbContext);
        var createUseCase = new CreateLeaveCategoryUseCase(repository);
        var updateUseCase = new UpdateLeaveCategoryUseCase(repository);

        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var created = await createUseCase.ExecuteAsync(
            new CreateLeaveCategoryRequest { Name = "مرخصی محدود", MaxDaysPerYear = 10 },
            orgId,
            userId);

        var updateRequest = new UpdateLeaveCategoryRequest
        {
            Name = "مرخصی بدون محدودیت",
            MaxDaysPerYear = null
        };

        var updated = await updateUseCase.ExecuteAsync(
            created.Id,
            updateRequest,
            userId,
            userOrganizationId: orgId,
            isSystemAdmin: false);

        Assert.Null(updated.MaxDaysPerYear);

        var dbCategory = await dbContext.LeaveCategories.FindAsync(created.Id);
        Assert.NotNull(dbCategory);
        Assert.Null(dbCategory.MaxDaysPerYear);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public async Task UpdateLeaveCategory_ZeroOrNegativeMaxDaysPerYear_ThrowsArgumentException(int invalidMaxDays)
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveCategoryRepository(dbContext);
        var createUseCase = new CreateLeaveCategoryUseCase(repository);
        var updateUseCase = new UpdateLeaveCategoryUseCase(repository);

        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var created = await createUseCase.ExecuteAsync(
            new CreateLeaveCategoryRequest { Name = "مرخصی تست", MaxDaysPerYear = 10 },
            orgId,
            userId);

        var updateRequest = new UpdateLeaveCategoryRequest
        {
            Name = "مرخصی تست",
            MaxDaysPerYear = invalidMaxDays
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            updateUseCase.ExecuteAsync(created.Id, updateRequest, userId, userOrganizationId: orgId, isSystemAdmin: false));

        Assert.Equal("حداکثر روز مرخصی در سال باید بزرگتر از صفر باشد. (Parameter 'MaxDaysPerYear')", ex.Message);
    }

    [Fact]
    public async Task CreateLeaveCategory_DuplicateNameInSameOrg_ThrowsArgumentException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveCategoryRepository(dbContext);
        var useCase = new CreateLeaveCategoryUseCase(repository);

        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var firstRequest = new CreateLeaveCategoryRequest { Name = "مرخصی استعلاجی" };
        await useCase.ExecuteAsync(firstRequest, orgId, userId);

        var duplicateRequest = new CreateLeaveCategoryRequest { Name = " مرخصی استعلاجی " };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ExecuteAsync(duplicateRequest, orgId, userId));
    }

    [Fact]
    public async Task CreateLeaveCategory_SameNameInDifferentOrg_CreatesCategorySuccessfully()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveCategoryRepository(dbContext);
        var useCase = new CreateLeaveCategoryUseCase(repository);

        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var requestOrgA = new CreateLeaveCategoryRequest { Name = "مرخصی استحقاقی" };
        var resultA = await useCase.ExecuteAsync(requestOrgA, orgA, userId);

        var requestOrgB = new CreateLeaveCategoryRequest { Name = "مرخصی استحقاقی" };
        var resultB = await useCase.ExecuteAsync(requestOrgB, orgB, userId);

        Assert.NotNull(resultA);
        Assert.NotNull(resultB);
        Assert.NotEqual(resultA.Id, resultB.Id);
        Assert.Equal(orgA, resultA.OrganizationId);
        Assert.Equal(orgB, resultB.OrganizationId);
    }

    [Fact]
    public async Task UpdateLeaveCategory_ValidInput_UpdatesCategoryProperties()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveCategoryRepository(dbContext);
        var createUseCase = new CreateLeaveCategoryUseCase(repository);
        var updateUseCase = new UpdateLeaveCategoryUseCase(repository);

        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var created = await createUseCase.ExecuteAsync(
            new CreateLeaveCategoryRequest { Name = "نام قدیمی", MaxDaysPerYear = 10 },
            orgId,
            userId);

        var updateRequest = new UpdateLeaveCategoryRequest
        {
            Name = "نام جدید",
            MaxDaysPerYear = 15,
            IsPaid = false,
            RequiresAttachment = true
        };

        var updated = await updateUseCase.ExecuteAsync(
            created.Id,
            updateRequest,
            userId,
            userOrganizationId: orgId,
            isSystemAdmin: false);

        Assert.Equal("نام جدید", updated.Name);
        Assert.Equal(15, updated.MaxDaysPerYear);
        Assert.False(updated.IsPaid);
        Assert.True(updated.RequiresAttachment);
        Assert.True(updated.IsActive); // IsActive preserved
        Assert.Equal(orgId, updated.OrganizationId); // OrganizationId preserved
    }

    [Fact]
    public async Task UpdateLeaveCategory_DuplicateNameInSameOrg_ThrowsArgumentException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveCategoryRepository(dbContext);
        var createUseCase = new CreateLeaveCategoryUseCase(repository);
        var updateUseCase = new UpdateLeaveCategoryUseCase(repository);

        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var cat1 = await createUseCase.ExecuteAsync(new CreateLeaveCategoryRequest { Name = "مرخصی A" }, orgId, userId);
        var cat2 = await createUseCase.ExecuteAsync(new CreateLeaveCategoryRequest { Name = "مرخصی B" }, orgId, userId);

        var updateRequest = new UpdateLeaveCategoryRequest { Name = "مرخصی A" };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            updateUseCase.ExecuteAsync(cat2.Id, updateRequest, userId, userOrganizationId: orgId, isSystemAdmin: false));
    }

    [Fact]
    public async Task UpdateLeaveCategory_CenterManagerUpdatingOtherOrgCategory_ThrowsUnauthorizedAccessException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveCategoryRepository(dbContext);
        var createUseCase = new CreateLeaveCategoryUseCase(repository);
        var updateUseCase = new UpdateLeaveCategoryUseCase(repository);

        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var created = await createUseCase.ExecuteAsync(new CreateLeaveCategoryRequest { Name = "مرخصی Org A" }, orgA, userId);

        var updateRequest = new UpdateLeaveCategoryRequest { Name = "تغییر نام غیرمجاز" };

        // CenterManager of Org B tries to update category of Org A
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            updateUseCase.ExecuteAsync(created.Id, updateRequest, userId, userOrganizationId: orgB, isSystemAdmin: false));
    }

    [Fact]
    public async Task ActivateAndDeactivateLeaveCategory_ChangesStateIdempotentlyAndPreservesRecord()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveCategoryRepository(dbContext);
        var createUseCase = new CreateLeaveCategoryUseCase(repository);
        var activateUseCase = new ActivateLeaveCategoryUseCase(repository);
        var deactivateUseCase = new DeactivateLeaveCategoryUseCase(repository);

        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var created = await createUseCase.ExecuteAsync(new CreateLeaveCategoryRequest { Name = "مرخصی ساعتی" }, orgId, userId);
        Assert.True(created.IsActive);

        // Deactivate
        var deactivated = await deactivateUseCase.ExecuteAsync(created.Id, userId, userOrganizationId: orgId, isSystemAdmin: false);
        Assert.False(deactivated.IsActive);

        // Deactivate again (idempotent)
        var deactivatedAgain = await deactivateUseCase.ExecuteAsync(created.Id, userId, userOrganizationId: orgId, isSystemAdmin: false);
        Assert.False(deactivatedAgain.IsActive);

        // Verify entity is NOT deleted from DB
        var dbRecord = await dbContext.LeaveCategories.FindAsync(created.Id);
        Assert.NotNull(dbRecord);
        Assert.False(dbRecord.IsDeleted);
        Assert.False(dbRecord.IsActive);

        // Activate
        var activated = await activateUseCase.ExecuteAsync(created.Id, userId, userOrganizationId: orgId, isSystemAdmin: false);
        Assert.True(activated.IsActive);

        // Activate again (idempotent)
        var activatedAgain = await activateUseCase.ExecuteAsync(created.Id, userId, userOrganizationId: orgId, isSystemAdmin: false);
        Assert.True(activatedAgain.IsActive);
    }

    [Fact]
    public async Task ActivateOrDeactivate_CenterManagerOtherOrg_ThrowsUnauthorizedAccessException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveCategoryRepository(dbContext);
        var createUseCase = new CreateLeaveCategoryUseCase(repository);
        var activateUseCase = new ActivateLeaveCategoryUseCase(repository);
        var deactivateUseCase = new DeactivateLeaveCategoryUseCase(repository);

        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var created = await createUseCase.ExecuteAsync(new CreateLeaveCategoryRequest { Name = "مرخصی بدون حقوق" }, orgA, userId);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            deactivateUseCase.ExecuteAsync(created.Id, userId, userOrganizationId: orgB, isSystemAdmin: false));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            activateUseCase.ExecuteAsync(created.Id, userId, userOrganizationId: orgB, isSystemAdmin: false));
    }

    [Fact]
    public async Task GetLeaveCategories_ReturnsActiveAndInactiveCategories_OrderedByNameAscending()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveCategoryRepository(dbContext);
        var getUseCase = new GetLeaveCategoriesUseCase(repository);

        var orgId = Guid.NewGuid();
        var catB = new LeaveCategory { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "B - مرخصی استعلاجی", IsActive = false, IsDeleted = false };
        var catA = new LeaveCategory { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "A - مرخصی استحقاقی", IsActive = true, IsDeleted = false };
        var catC = new LeaveCategory { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "C - مرخصی ساعتی", IsActive = true, IsDeleted = false };

        dbContext.LeaveCategories.AddRange(catB, catA, catC);
        await dbContext.SaveChangesAsync();

        var result = (await getUseCase.ExecuteAsync(orgId)).ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal("A - مرخصی استحقاقی", result[0].Name);
        Assert.Equal("B - مرخصی استعلاجی", result[1].Name);
        Assert.Equal("C - مرخصی ساعتی", result[2].Name);
        Assert.False(result[1].IsActive); // Contains inactive category
    }

    [Fact]
    public async Task GetLeaveCategories_ExcludesDeletedCategoriesAndOtherOrgCategories()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveCategoryRepository(dbContext);
        var getUseCase = new GetLeaveCategoriesUseCase(repository);

        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        var catOrgA = new LeaveCategory { Id = Guid.NewGuid(), OrganizationId = orgA, Name = "مرخصی Org A", IsActive = true, IsDeleted = false };
        var catOrgADeleted = new LeaveCategory { Id = Guid.NewGuid(), OrganizationId = orgA, Name = "مرخصی حذف شده", IsActive = true, IsDeleted = true };
        var catOrgB = new LeaveCategory { Id = Guid.NewGuid(), OrganizationId = orgB, Name = "مرخصی Org B", IsActive = true, IsDeleted = false };

        dbContext.LeaveCategories.AddRange(catOrgA, catOrgADeleted, catOrgB);
        await dbContext.SaveChangesAsync();

        var resultOrgA = (await getUseCase.ExecuteAsync(orgA)).ToList();

        Assert.Single(resultOrgA);
        Assert.Equal(catOrgA.Id, resultOrgA[0].Id);
    }

    [Fact]
    public async Task GetLeaveCategoriesUseCase_EmptyOrgId_ThrowsArgumentException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveCategoryRepository(dbContext);
        var getUseCase = new GetLeaveCategoriesUseCase(repository);

        await Assert.ThrowsAsync<ArgumentException>(() => getUseCase.ExecuteAsync(Guid.Empty));
    }

    [Fact]
    public async Task Controller_GetLeaveCategories_AsEmployee_ReturnsOrgLeaveCategories()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveCategoryRepository(dbContext);
        var getUseCase = new GetLeaveCategoriesUseCase(repository);
        var createUseCase = new CreateLeaveCategoryUseCase(repository);
        var updateUseCase = new UpdateLeaveCategoryUseCase(repository);
        var activateUseCase = new ActivateLeaveCategoryUseCase(repository);
        var deactivateUseCase = new DeactivateLeaveCategoryUseCase(repository);

        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await createUseCase.ExecuteAsync(new CreateLeaveCategoryRequest { Name = "مرخصی سالانه" }, orgId, userId);

        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "Employee"),
            new Claim("organization_id", orgId.ToString())
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var currentUserService = new CurrentUserService(httpContextAccessor);

        var getByIdUseCase = new GetLeaveCategoryByIdUseCase(repository);

        var controller = new LeaveCategoriesController(
            createUseCase,
            getUseCase,
            getByIdUseCase,
            updateUseCase,
            activateUseCase,
            deactivateUseCase,
            currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var actionResult = await controller.GetLeaveCategories(null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var dtos = Assert.IsAssignableFrom<IEnumerable<LeaveCategoryDto>>(okResult.Value);
        Assert.Single(dtos);
        Assert.Equal("مرخصی سالانه", dtos.First().Name);
    }

    [Fact]
    public async Task Controller_GetLeaveCategories_AsCenterManagerAccessingOtherOrg_ReturnsForbid()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveCategoryRepository(dbContext);
        var getUseCase = new GetLeaveCategoriesUseCase(repository);
        var createUseCase = new CreateLeaveCategoryUseCase(repository);
        var updateUseCase = new UpdateLeaveCategoryUseCase(repository);
        var activateUseCase = new ActivateLeaveCategoryUseCase(repository);
        var deactivateUseCase = new DeactivateLeaveCategoryUseCase(repository);

        var myOrgId = Guid.NewGuid();
        var otherOrgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "CenterManager"),
            new Claim("organization_id", myOrgId.ToString())
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var currentUserService = new CurrentUserService(httpContextAccessor);

        var getByIdUseCase = new GetLeaveCategoryByIdUseCase(repository);

        var controller = new LeaveCategoriesController(
            createUseCase,
            getUseCase,
            getByIdUseCase,
            updateUseCase,
            activateUseCase,
            deactivateUseCase,
            currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var actionResult = await controller.GetLeaveCategories(otherOrgId, CancellationToken.None);

        Assert.IsType<ForbidResult>(actionResult);
    }

    [Fact]
    public async Task Controller_GetLeaveCategories_AsSystemAdminWithOrgParam_ReturnsRequestedOrgCategories()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveCategoryRepository(dbContext);
        var getUseCase = new GetLeaveCategoriesUseCase(repository);
        var createUseCase = new CreateLeaveCategoryUseCase(repository);
        var updateUseCase = new UpdateLeaveCategoryUseCase(repository);
        var activateUseCase = new ActivateLeaveCategoryUseCase(repository);
        var deactivateUseCase = new DeactivateLeaveCategoryUseCase(repository);

        var targetOrgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await createUseCase.ExecuteAsync(new CreateLeaveCategoryRequest { Name = "مرخصی ادمین" }, targetOrgId, userId);

        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "SystemAdmin")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var currentUserService = new CurrentUserService(httpContextAccessor);

        var getByIdUseCase = new GetLeaveCategoryByIdUseCase(repository);

        var controller = new LeaveCategoriesController(
            createUseCase,
            getUseCase,
            getByIdUseCase,
            updateUseCase,
            activateUseCase,
            deactivateUseCase,
            currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var actionResult = await controller.GetLeaveCategories(targetOrgId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var dtos = Assert.IsAssignableFrom<IEnumerable<LeaveCategoryDto>>(okResult.Value);
        Assert.Single(dtos);
        Assert.Equal("مرخصی ادمین", dtos.First().Name);
    }

    [Fact]
    public async Task GetLeaveCategoryById_ValidIdAndSameOrg_ReturnsCategory()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveCategoryRepository(dbContext);
        var createUseCase = new CreateLeaveCategoryUseCase(repository);
        var getByIdUseCase = new GetLeaveCategoryByIdUseCase(repository);

        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var created = await createUseCase.ExecuteAsync(new CreateLeaveCategoryRequest { Name = "مرخصی ساعتی" }, orgId, userId);

        var result = await getByIdUseCase.ExecuteAsync(created.Id, userOrganizationId: orgId, isSystemAdmin: false);

        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal("مرخصی ساعتی", result.Name);
    }

    [Fact]
    public async Task GetLeaveCategoryById_NonExistentOrDeletedId_ReturnsNull()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveCategoryRepository(dbContext);
        var getByIdUseCase = new GetLeaveCategoryByIdUseCase(repository);

        var orgId = Guid.NewGuid();

        var result = await getByIdUseCase.ExecuteAsync(Guid.NewGuid(), userOrganizationId: orgId, isSystemAdmin: false);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLeaveCategoryById_DifferentOrg_ThrowsUnauthorizedAccessException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveCategoryRepository(dbContext);
        var createUseCase = new CreateLeaveCategoryUseCase(repository);
        var getByIdUseCase = new GetLeaveCategoryByIdUseCase(repository);

        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var created = await createUseCase.ExecuteAsync(new CreateLeaveCategoryRequest { Name = "مرخصی A" }, orgA, userId);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            getByIdUseCase.ExecuteAsync(created.Id, userOrganizationId: orgB, isSystemAdmin: false));
    }

    [Fact]
    public async Task GetLeaveCategoryById_SystemAdminDifferentOrg_ReturnsCategory()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveCategoryRepository(dbContext);
        var createUseCase = new CreateLeaveCategoryUseCase(repository);
        var getByIdUseCase = new GetLeaveCategoryByIdUseCase(repository);

        var orgA = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var created = await createUseCase.ExecuteAsync(new CreateLeaveCategoryRequest { Name = "مرخصی A" }, orgA, userId);

        var result = await getByIdUseCase.ExecuteAsync(created.Id, userOrganizationId: Guid.Empty, isSystemAdmin: true);

        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
    }

    [Fact]
    public async Task Controller_GetLeaveCategoryById_AsCenterManager_ReturnsCategory()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveCategoryRepository(dbContext);
        var getUseCase = new GetLeaveCategoriesUseCase(repository);
        var getByIdUseCase = new GetLeaveCategoryByIdUseCase(repository);
        var createUseCase = new CreateLeaveCategoryUseCase(repository);
        var updateUseCase = new UpdateLeaveCategoryUseCase(repository);
        var activateUseCase = new ActivateLeaveCategoryUseCase(repository);
        var deactivateUseCase = new DeactivateLeaveCategoryUseCase(repository);

        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var created = await createUseCase.ExecuteAsync(new CreateLeaveCategoryRequest { Name = "مرخصی سالانه" }, orgId, userId);

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

        var controller = new LeaveCategoriesController(
            createUseCase,
            getUseCase,
            getByIdUseCase,
            updateUseCase,
            activateUseCase,
            deactivateUseCase,
            currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var actionResult = await controller.GetLeaveCategoryById(created.Id, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var dto = Assert.IsType<LeaveCategoryDto>(okResult.Value);
        Assert.Equal(created.Id, dto.Id);
        Assert.Equal("مرخصی سالانه", dto.Name);
    }
}

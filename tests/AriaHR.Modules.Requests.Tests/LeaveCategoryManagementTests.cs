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
    public async Task Controller_GetLeaveCategories_AsEmployee_ReturnsActiveOrgLeaveCategoriesOnly()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveCategoryRepository(dbContext);
        var getUseCase = new GetLeaveCategoriesUseCase(repository);
        var createUseCase = new CreateLeaveCategoryUseCase(repository);
        var updateUseCase = new UpdateLeaveCategoryUseCase(repository);
        var activateUseCase = new ActivateLeaveCategoryUseCase(repository);
        var deactivateUseCase = new DeactivateLeaveCategoryUseCase(repository);

        var orgId = Guid.NewGuid();
        var otherOrgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var activeCat = await createUseCase.ExecuteAsync(new CreateLeaveCategoryRequest { Name = "مرخصی سالانه فعال" }, orgId, userId);
        var inactiveCat = await createUseCase.ExecuteAsync(new CreateLeaveCategoryRequest { Name = "مرخصی غیرفعال" }, orgId, userId);
        await deactivateUseCase.ExecuteAsync(inactiveCat.Id, userId, orgId, false);
        await createUseCase.ExecuteAsync(new CreateLeaveCategoryRequest { Name = "مرخصی سازمان دیگر" }, otherOrgId, userId);

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

        // 1. Without organizationId query parameter
        var actionResult = await controller.GetLeaveCategories(null, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var dtos = Assert.IsAssignableFrom<IEnumerable<LeaveCategoryListItemDto>>(okResult.Value).ToList();
        Assert.Single(dtos);
        Assert.Equal("مرخصی سالانه فعال", dtos.First().Name);

        // 2. With matching organizationId query parameter
        var actionResultWithOrg = await controller.GetLeaveCategories(orgId, CancellationToken.None);
        var okResultWithOrg = Assert.IsType<OkObjectResult>(actionResultWithOrg);
        var dtosWithOrg = Assert.IsAssignableFrom<IEnumerable<LeaveCategoryListItemDto>>(okResultWithOrg.Value).ToList();
        Assert.Single(dtosWithOrg);
        Assert.Equal("مرخصی سالانه فعال", dtosWithOrg.First().Name);

        // 3. With different organizationId query parameter
        var actionResultOtherOrg = await controller.GetLeaveCategories(otherOrgId, CancellationToken.None);
        var objectResult = Assert.IsType<ObjectResult>(actionResultOtherOrg);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("ORGANIZATION_ACCESS_DENIED", problem.Extensions["errorCode"]);
    }

    [Fact]
    public async Task Controller_GetLeaveCategories_AsEmployee_OrganizationWithNoActiveCategories_ReturnsEmptyArray()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveCategoryRepository(dbContext);
        var getUseCase = new GetLeaveCategoriesUseCase(repository);
        var createUseCase = new CreateLeaveCategoryUseCase(repository);
        var updateUseCase = new UpdateLeaveCategoryUseCase(repository);
        var activateUseCase = new ActivateLeaveCategoryUseCase(repository);
        var deactivateUseCase = new DeactivateLeaveCategoryUseCase(repository);

        var orgId = Guid.NewGuid();
        var otherOrgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Create inactive category in employee's org
        var inactiveCat = await createUseCase.ExecuteAsync(new CreateLeaveCategoryRequest { Name = "مرخصی غیرفعال" }, orgId, userId);
        await deactivateUseCase.ExecuteAsync(inactiveCat.Id, userId, orgId, false);

        // Create active category in another org
        await createUseCase.ExecuteAsync(new CreateLeaveCategoryRequest { Name = "مرخصی فعال سازمان دیگر" }, otherOrgId, userId);

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
        var dtos = Assert.IsAssignableFrom<IEnumerable<LeaveCategoryListItemDto>>(okResult.Value).ToList();

        Assert.Empty(dtos);
    }

    [Fact]
    public async Task Controller_GetLeaveCategories_AsEmployee_ExcludesInactiveAndOtherOrgCategories()
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

        // 1. Active category in my org (should be returned)
        var myActiveCat = await createUseCase.ExecuteAsync(new CreateLeaveCategoryRequest { Name = "مرخصی سالانه فعال" }, myOrgId, userId);

        // 2. Inactive category in my org (should be excluded)
        var myInactiveCat = await createUseCase.ExecuteAsync(new CreateLeaveCategoryRequest { Name = "مرخصی استعلاجی غیرفعال" }, myOrgId, userId);
        await deactivateUseCase.ExecuteAsync(myInactiveCat.Id, userId, myOrgId, false);

        // 3. Active category in another org (should be excluded)
        await createUseCase.ExecuteAsync(new CreateLeaveCategoryRequest { Name = "مرخصی فعال سازمان B" }, otherOrgId, userId);

        // 4. Inactive category in another org (should be excluded)
        var otherInactiveCat = await createUseCase.ExecuteAsync(new CreateLeaveCategoryRequest { Name = "مرخصی غیرفعال سازمان B" }, otherOrgId, userId);
        await deactivateUseCase.ExecuteAsync(otherInactiveCat.Id, userId, otherOrgId, false);

        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "Employee"),
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

        var actionResult = await controller.GetLeaveCategories(null, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var dtos = Assert.IsAssignableFrom<IEnumerable<LeaveCategoryListItemDto>>(okResult.Value).ToList();

        Assert.Single(dtos);
        Assert.Equal(myActiveCat.Id, dtos.First().Id);
        Assert.Equal("مرخصی سالانه فعال", dtos.First().Name);
        Assert.Equal(myOrgId, dtos.First().OrganizationId);
    }

    [Fact]
    public async Task Controller_GetLeaveCategories_AsSystemAdmin_ReturnsActiveAndInactiveCategoriesForRequestedOrg()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveCategoryRepository(dbContext);
        var getUseCase = new GetLeaveCategoriesUseCase(repository);
        var createUseCase = new CreateLeaveCategoryUseCase(repository);
        var updateUseCase = new UpdateLeaveCategoryUseCase(repository);
        var activateUseCase = new ActivateLeaveCategoryUseCase(repository);
        var deactivateUseCase = new DeactivateLeaveCategoryUseCase(repository);

        var targetOrgId = Guid.NewGuid();
        var otherOrgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Target org categories (1 active, 1 inactive)
        var activeCat = await createUseCase.ExecuteAsync(new CreateLeaveCategoryRequest { Name = "مرخصی A فعال" }, targetOrgId, userId);
        var inactiveCat = await createUseCase.ExecuteAsync(new CreateLeaveCategoryRequest { Name = "مرخصی B غیرفعال" }, targetOrgId, userId);
        await deactivateUseCase.ExecuteAsync(inactiveCat.Id, userId, targetOrgId, false);

        // Other org category
        await createUseCase.ExecuteAsync(new CreateLeaveCategoryRequest { Name = "مرخصی سازمان دیگر" }, otherOrgId, userId);

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
        var dtos = Assert.IsAssignableFrom<IEnumerable<LeaveCategoryListItemDto>>(okResult.Value).ToList();

        Assert.Equal(2, dtos.Count);
        Assert.Contains(dtos, d => d.Id == activeCat.Id);
        Assert.Contains(dtos, d => d.Id == inactiveCat.Id);
    }

    [Fact]
    public async Task Controller_GetLeaveCategories_MissingOrgClaim_Returns403ForbiddenWithDiagnostic()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveCategoryRepository(dbContext);
        var getUseCase = new GetLeaveCategoriesUseCase(repository);
        var createUseCase = new CreateLeaveCategoryUseCase(repository);
        var updateUseCase = new UpdateLeaveCategoryUseCase(repository);
        var activateUseCase = new ActivateLeaveCategoryUseCase(repository);
        var deactivateUseCase = new DeactivateLeaveCategoryUseCase(repository);

        var userId = Guid.NewGuid();

        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "Employee")
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

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("ORGANIZATION_CLAIM_MISSING", problem.Extensions["errorCode"]);
    }

    [Fact]
    public async Task Controller_GetLeaveCategories_Unauthenticated_Returns401UnauthorizedWithDiagnostic()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveCategoryRepository(dbContext);
        var getUseCase = new GetLeaveCategoriesUseCase(repository);
        var createUseCase = new CreateLeaveCategoryUseCase(repository);
        var updateUseCase = new UpdateLeaveCategoryUseCase(repository);
        var activateUseCase = new ActivateLeaveCategoryUseCase(repository);
        var deactivateUseCase = new DeactivateLeaveCategoryUseCase(repository);

        var httpContext = new DefaultHttpContext(); // No user identity
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

        var unauthResult = Assert.IsType<UnauthorizedObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(unauthResult.Value);
        Assert.Equal("USER_ID_MISSING", problem.Extensions["errorCode"]);
    }

    [Fact]
    public async Task Controller_GetLeaveCategories_AsCenterManager_ReturnsActiveAndInactiveCategories()
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

        var activeCat = await createUseCase.ExecuteAsync(new CreateLeaveCategoryRequest { Name = "مرخصی استحقاقی" }, orgId, userId);
        var inactiveCat = await createUseCase.ExecuteAsync(new CreateLeaveCategoryRequest { Name = "مرخصی استعلاجی" }, orgId, userId);
        await deactivateUseCase.ExecuteAsync(inactiveCat.Id, userId, orgId, false);

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
        var dtos = Assert.IsAssignableFrom<IEnumerable<LeaveCategoryListItemDto>>(okResult.Value).ToList();
        Assert.Equal(2, dtos.Count); // CenterManager sees both active and inactive categories
    }

    [Fact]
    public async Task Controller_GetLeaveCategories_AsCenterManagerAccessingOtherOrg_Returns403Forbidden()
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

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("ORGANIZATION_ACCESS_DENIED", problem.Extensions["errorCode"]);
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
        var dtos = Assert.IsAssignableFrom<IEnumerable<LeaveCategoryListItemDto>>(okResult.Value);
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

    [Fact]
    public void LeaveCategoryListItemDto_ContainsOnlyRequiredProperties()
    {
        var properties = typeof(LeaveCategoryListItemDto).GetProperties();
        var propertyNames = properties.Select(p => p.Name).OrderBy(p => p).ToList();

        Assert.Equal(4, properties.Length);
        Assert.Equal(new[] { "Id", "IsActive", "Name", "OrganizationId" }, propertyNames);
    }

    [Fact]
    public async Task GetLeaveCategories_ReturnsSimplifiedDtoMatchingDomainEntity()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveCategoryRepository(dbContext);
        var useCase = new GetLeaveCategoriesUseCase(repository);

        var orgId = Guid.NewGuid();
        var category = new LeaveCategory
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Name = "مرخصی تست",
            MaxDaysPerYear = 15,
            IsPaid = true,
            RequiresAttachment = true,
            IsActive = true
        };

        dbContext.LeaveCategories.Add(category);
        await dbContext.SaveChangesAsync();

        var result = (await useCase.ExecuteAsync(orgId)).ToList();

        Assert.Single(result);
        var item = result.First();
        Assert.Equal(category.Id, item.Id);
        Assert.Equal(category.OrganizationId, item.OrganizationId);
        Assert.Equal(category.Name, item.Name);
        Assert.True(item.IsActive);
    }

    [Fact]
    public void LeaveCategoryListItemDto_SerializesToJsonWithIsActiveBooleanProperty()
    {
        var dto = new LeaveCategoryListItemDto
        {
            Id = Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
            OrganizationId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
            Name = "Annual Leave",
            IsActive = true
        };

        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        };

        var json = System.Text.Json.JsonSerializer.Serialize(dto, jsonOptions);

        Assert.Contains("\"isActive\":true", json);

        using var jsonDoc = System.Text.Json.JsonDocument.Parse(json);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("isActive", out var isActiveProp));
        Assert.Equal(System.Text.Json.JsonValueKind.True, isActiveProp.ValueKind);
    }

    [Fact]
    public async Task Controller_GetLeaveCategories_ReturnsIsActiveInResponseForBothActiveAndInactiveItems()
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

        var activeCat = await createUseCase.ExecuteAsync(new CreateLeaveCategoryRequest { Name = "مرخصی فعال" }, orgId, userId);
        var inactiveCat = await createUseCase.ExecuteAsync(new CreateLeaveCategoryRequest { Name = "مرخصی غیرفعال" }, orgId, userId);
        await deactivateUseCase.ExecuteAsync(inactiveCat.Id, userId, orgId, false);

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
        var dtos = Assert.IsAssignableFrom<IEnumerable<LeaveCategoryListItemDto>>(okResult.Value).ToList();

        Assert.Equal(2, dtos.Count);

        var activeDto = dtos.Single(d => d.Id == activeCat.Id);
        Assert.True(activeDto.IsActive);

        var inactiveDto = dtos.Single(d => d.Id == inactiveCat.Id);
        Assert.False(inactiveDto.IsActive);
    }
}

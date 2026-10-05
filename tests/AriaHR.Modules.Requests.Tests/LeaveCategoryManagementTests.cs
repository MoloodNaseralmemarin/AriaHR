using AriaHR.Modules.Requests.Application.DTOs;
using AriaHR.Modules.Requests.Application.UseCases.ActivateLeaveCategory;
using AriaHR.Modules.Requests.Application.UseCases.CreateLeaveCategory;
using AriaHR.Modules.Requests.Application.UseCases.DeactivateLeaveCategory;
using AriaHR.Modules.Requests.Application.UseCases.UpdateLeaveCategory;
using AriaHR.Modules.Requests.Domain.Entities;
using AriaHR.Modules.Requests.Infrastructure.Persistence;
using AriaHR.Modules.Requests.Infrastructure.Repositories;
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
}

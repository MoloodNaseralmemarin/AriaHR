using AriaHR.Modules.Requests.Application.DTOs;
using AriaHR.Modules.Requests.Application.UseCases.ChangeLeaveTypeStatus;
using AriaHR.Modules.Requests.Application.UseCases.CreateLeaveType;
using AriaHR.Modules.Requests.Application.UseCases.GetLeaveTypeById;
using AriaHR.Modules.Requests.Application.UseCases.GetOrganizationLeaveTypes;
using AriaHR.Modules.Requests.Application.UseCases.UpdateLeaveType;
using AriaHR.Modules.Requests.Domain.Entities;
using AriaHR.Modules.Requests.Infrastructure.Persistence;
using AriaHR.Modules.Requests.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AriaHR.Modules.Requests.Tests;

public class LeaveTypeTests
{
    private RequestsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<RequestsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new RequestsDbContext(options);
    }

    [Fact]
    public async Task CreateLeaveType_ValidData_CreatesActiveLeaveType()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveTypeRepository(dbContext);
        var useCase = new CreateLeaveTypeUseCase(repository);

        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var request = new CreateLeaveTypeRequest
        {
            Name = " استحقاقی ",
            MaxDaysPerYear = 26,
            IsPaid = true,
            RequiresAttachment = false
        };

        var response = await useCase.ExecuteAsync(request, orgId, userId);

        Assert.NotNull(response);
        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal("استحقاقی", response.Name); // Trimmed
        Assert.Equal(26, response.MaxDaysPerYear);
        Assert.True(response.IsPaid);
        Assert.False(response.RequiresAttachment);
        Assert.True(response.IsActive); // Active by default
    }

    [Fact]
    public async Task CreateLeaveType_NegativeMaxDays_ThrowsArgumentException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveTypeRepository(dbContext);
        var useCase = new CreateLeaveTypeUseCase(repository);

        var request = new CreateLeaveTypeRequest
        {
            Name = "استعلاجی",
            MaxDaysPerYear = -1,
            IsPaid = false,
            RequiresAttachment = true
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ExecuteAsync(request, Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateLeaveType_EmptyName_ThrowsArgumentException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveTypeRepository(dbContext);
        var useCase = new CreateLeaveTypeUseCase(repository);

        var request = new CreateLeaveTypeRequest
        {
            Name = "   ",
            MaxDaysPerYear = 10
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ExecuteAsync(request, Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateLeaveType_DuplicateNameInSameOrganization_ThrowsInvalidOperationException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveTypeRepository(dbContext);
        var useCase = new CreateLeaveTypeUseCase(repository);

        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var request1 = new CreateLeaveTypeRequest { Name = "استحقاقی", MaxDaysPerYear = 26 };
        await useCase.ExecuteAsync(request1, orgId, userId);

        // Case-insensitive duplicate attempt in same organization
        var request2 = new CreateLeaveTypeRequest { Name = "  استحقاقی  ", MaxDaysPerYear = 30 };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.ExecuteAsync(request2, orgId, userId));
    }

    [Fact]
    public async Task CreateLeaveType_SameNameInDifferentOrganizations_Succeeds()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveTypeRepository(dbContext);
        var useCase = new CreateLeaveTypeUseCase(repository);

        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var requestA = new CreateLeaveTypeRequest { Name = "استحقاقی", MaxDaysPerYear = 26 };
        var requestB = new CreateLeaveTypeRequest { Name = "استحقاقی", MaxDaysPerYear = 30 };

        var responseA = await useCase.ExecuteAsync(requestA, orgA, userId);
        var responseB = await useCase.ExecuteAsync(requestB, orgB, userId);

        Assert.NotNull(responseA);
        Assert.NotNull(responseB);
        Assert.NotEqual(responseA.Id, responseB.Id);
    }

    [Fact]
    public async Task GetOrganizationLeaveTypes_ReturnsAllNonDeletedActiveAndInactive()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveTypeRepository(dbContext);

        var orgId = Guid.NewGuid();
        var otherOrgId = Guid.NewGuid();

        await repository.AddAsync(new LeaveType { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "A", IsActive = true });
        await repository.AddAsync(new LeaveType { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "B", IsActive = false });
        await repository.AddAsync(new LeaveType { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "C", IsActive = true, IsDeleted = true }); // Soft deleted
        await repository.AddAsync(new LeaveType { Id = Guid.NewGuid(), OrganizationId = otherOrgId, Name = "D", IsActive = true });

        var useCase = new GetOrganizationLeaveTypesUseCase(repository);

        var result = (await useCase.ExecuteAsync(orgId)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.Name == "A" && x.IsActive);
        Assert.Contains(result, x => x.Name == "B" && !x.IsActive);
    }

    [Fact]
    public async Task GetLeaveTypeById_SameOrg_ReturnsLeaveType()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveTypeRepository(dbContext);

        var orgId = Guid.NewGuid();
        var leaveTypeId = Guid.NewGuid();

        await repository.AddAsync(new LeaveType { Id = leaveTypeId, OrganizationId = orgId, Name = "استحقاقی", MaxDaysPerYear = 20, IsActive = true });

        var useCase = new GetLeaveTypeByIdUseCase(repository);

        var result = await useCase.ExecuteAsync(leaveTypeId, orgId);

        Assert.NotNull(result);
        Assert.Equal(leaveTypeId, result.Id);
        Assert.Equal("استحقاقی", result.Name);
    }

    [Fact]
    public async Task GetLeaveTypeById_DifferentOrg_ReturnsNull()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveTypeRepository(dbContext);

        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        var leaveTypeId = Guid.NewGuid();

        await repository.AddAsync(new LeaveType { Id = leaveTypeId, OrganizationId = orgA, Name = "استحقاقی", MaxDaysPerYear = 20, IsActive = true });

        var useCase = new GetLeaveTypeByIdUseCase(repository);

        var result = await useCase.ExecuteAsync(leaveTypeId, orgB);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateLeaveType_ValidData_PreservesOrganizationIdAndIsActive()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveTypeRepository(dbContext);

        var orgId = Guid.NewGuid();
        var leaveTypeId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var initialEntity = new LeaveType
        {
            Id = leaveTypeId,
            OrganizationId = orgId,
            Name = "استحقاقی قدیمی",
            MaxDaysPerYear = 20,
            IsPaid = true,
            RequiresAttachment = false,
            IsActive = false // Inactive
        };
        await repository.AddAsync(initialEntity);

        var updateUseCase = new UpdateLeaveTypeUseCase(repository);

        var updateRequest = new UpdateLeaveTypeRequest
        {
            Name = "مرخصی استحقاقی جدید",
            MaxDaysPerYear = 25,
            IsPaid = false,
            RequiresAttachment = true
        };

        var response = await updateUseCase.ExecuteAsync(leaveTypeId, updateRequest, orgId, userId);

        Assert.Equal("مرخصی استحقاقی جدید", response.Name);
        Assert.Equal(25, response.MaxDaysPerYear);
        Assert.False(response.IsPaid);
        Assert.True(response.RequiresAttachment);
        Assert.False(response.IsActive); // IsActive preserved!

        var entityInDb = await repository.GetByIdAndOrganizationIdAsync(leaveTypeId, orgId);
        Assert.NotNull(entityInDb);
        Assert.Equal(orgId, entityInDb.OrganizationId); // OrganizationId preserved!
    }

    [Fact]
    public async Task UpdateLeaveType_OtherOrg_ThrowsKeyNotFoundException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveTypeRepository(dbContext);

        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        var leaveTypeId = Guid.NewGuid();

        await repository.AddAsync(new LeaveType { Id = leaveTypeId, OrganizationId = orgA, Name = "A", IsActive = true });

        var updateUseCase = new UpdateLeaveTypeUseCase(repository);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            updateUseCase.ExecuteAsync(leaveTypeId, new UpdateLeaveTypeRequest { Name = "B" }, orgB, Guid.NewGuid()));
    }

    [Fact]
    public async Task ChangeLeaveTypeStatus_TogglesActiveState()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveTypeRepository(dbContext);

        var orgId = Guid.NewGuid();
        var leaveTypeId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await repository.AddAsync(new LeaveType { Id = leaveTypeId, OrganizationId = orgId, Name = "استعلاجی", IsActive = true });

        var statusUseCase = new ChangeLeaveTypeStatusUseCase(repository);

        // Deactivate
        var deactivated = await statusUseCase.ExecuteAsync(leaveTypeId, new ChangeLeaveTypeStatusRequest { IsActive = false }, orgId, userId);
        Assert.False(deactivated.IsActive);

        // Reactivate
        var reactivated = await statusUseCase.ExecuteAsync(leaveTypeId, new ChangeLeaveTypeStatusRequest { IsActive = true }, orgId, userId);
        Assert.True(reactivated.IsActive);
    }
}

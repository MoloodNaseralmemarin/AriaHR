using AriaHR.Modules.Requests.Application.DTOs;
using AriaHR.Shared.Services;
using AriaHR.Modules.Requests.Application.UseCases.ApproveLeaveRequest;
using AriaHR.Modules.Requests.Application.UseCases.CancelLeaveRequest;
using AriaHR.Modules.Requests.Application.UseCases.RejectLeaveRequest;
using AriaHR.Modules.Requests.Domain.Entities;
using AriaHR.Modules.Requests.Domain.Enums;
using AriaHR.Modules.Requests.Infrastructure.Persistence;
using AriaHR.Modules.Requests.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;
using LeaveTypeEnum = AriaHR.Modules.Requests.Domain.Enums.LeaveType;

namespace AriaHR.Modules.Requests.Tests;

public class LeaveRequestStateTransitionTests
{
    private RequestsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<RequestsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new RequestsDbContext(options);
    }

    private class SingleEmployeeLookupService : IEmployeeLookupService
    {
        private readonly Guid _employeeId;
        private readonly Guid _organizationId;

        public SingleEmployeeLookupService(Guid employeeId, Guid organizationId)
        {
            _employeeId = employeeId;
            _organizationId = organizationId;
        }

        public Task<(Guid EmployeeId, Guid OrganizationId)?> GetEmployeeDetailsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<(Guid, Guid)?>((_employeeId, _organizationId));
        }
    }

    [Fact]
    public async Task StateTransition_PendingToApproved_Succeeds()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var managerUser = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        var request = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            OrganizationId = orgId,
            LeaveType = LeaveTypeEnum.Daily,
            Date = new DateOnly(2026, 10, 5),
            Status = RequestStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
        await repository.AddAsync(request);
        await repository.SaveChangesAsync();

        var useCase = new ApproveLeaveRequestUseCase(repository);

        var result = await useCase.ExecuteAsync(request.Id, managerUser, orgId, isSystemAdmin: false);

        Assert.Equal(RequestStatus.Approved, result.Status);
        Assert.NotNull(result.ApprovedAt);
        Assert.Equal(managerUser, result.ApprovedBy);
    }

    [Fact]
    public async Task StateTransition_PendingToRejected_Succeeds()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var managerUser = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        var request = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            OrganizationId = orgId,
            LeaveType = LeaveTypeEnum.Daily,
            Date = new DateOnly(2026, 10, 5),
            Status = RequestStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
        await repository.AddAsync(request);
        await repository.SaveChangesAsync();

        var useCase = new RejectLeaveRequestUseCase(repository);

        var result = await useCase.ExecuteAsync(
            request.Id,
            new RejectLeaveRequestRequest { Reason = "Staff shortage" },
            managerUser,
            orgId,
            isSystemAdmin: false);

        Assert.Equal(RequestStatus.Rejected, result.Status);
        Assert.Equal("Staff shortage", result.RejectedReason);
    }

    [Fact]
    public async Task StateTransition_PendingToCancelled_Succeeds()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var employeeUser = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var lookupService = new SingleEmployeeLookupService(employeeId, orgId);

        var request = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            OrganizationId = orgId,
            LeaveType = LeaveTypeEnum.Daily,
            Date = new DateOnly(2026, 10, 5),
            Status = RequestStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
        await repository.AddAsync(request);
        await repository.SaveChangesAsync();

        var useCase = new CancelLeaveRequestUseCase(repository, lookupService);

        await useCase.ExecuteAsync(request.Id, employeeUser);

        var updated = await repository.GetByIdAsync(request.Id);
        Assert.NotNull(updated);
        Assert.Equal(RequestStatus.Cancelled, updated.Status);
    }

    [Fact]
    public async Task StateTransition_ApprovedToRejected_ThrowsInvalidOperationException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var managerUser = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        var request = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            OrganizationId = orgId,
            LeaveType = LeaveTypeEnum.Daily,
            Date = new DateOnly(2026, 10, 5),
            Status = RequestStatus.Approved, // Already approved
            CreatedAtUtc = DateTime.UtcNow
        };
        await repository.AddAsync(request);
        await repository.SaveChangesAsync();

        var useCase = new RejectLeaveRequestUseCase(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.ExecuteAsync(
                request.Id,
                new RejectLeaveRequestRequest { Reason = "Cannot reject approved" },
                managerUser,
                orgId,
                isSystemAdmin: false));
    }

    [Fact]
    public async Task StateTransition_RejectedToApproved_ThrowsInvalidOperationException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var managerUser = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        var request = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            OrganizationId = orgId,
            LeaveType = LeaveTypeEnum.Daily,
            Date = new DateOnly(2026, 10, 5),
            Status = RequestStatus.Rejected, // Already rejected
            CreatedAtUtc = DateTime.UtcNow
        };
        await repository.AddAsync(request);
        await repository.SaveChangesAsync();

        var useCase = new ApproveLeaveRequestUseCase(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.ExecuteAsync(request.Id, managerUser, orgId, isSystemAdmin: false));
    }

    [Fact]
    public async Task StateTransition_CancelledToApproved_ThrowsInvalidOperationException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var managerUser = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        var request = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            OrganizationId = orgId,
            LeaveType = LeaveTypeEnum.Daily,
            Date = new DateOnly(2026, 10, 5),
            Status = RequestStatus.Cancelled, // Already cancelled
            CreatedAtUtc = DateTime.UtcNow
        };
        await repository.AddAsync(request);
        await repository.SaveChangesAsync();

        var useCase = new ApproveLeaveRequestUseCase(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.ExecuteAsync(request.Id, managerUser, orgId, isSystemAdmin: false));
    }
}

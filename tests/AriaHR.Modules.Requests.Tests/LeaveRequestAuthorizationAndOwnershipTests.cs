using AriaHR.Modules.Requests.Application.DTOs;
using AriaHR.Shared.Services;
using AriaHR.Modules.Requests.Application.UseCases.ApproveLeaveRequest;
using AriaHR.Modules.Requests.Application.UseCases.CancelLeaveRequest;
using AriaHR.Modules.Requests.Application.UseCases.GetLeaveRequestById;
using AriaHR.Modules.Requests.Application.UseCases.RejectLeaveRequest;
using AriaHR.Modules.Requests.Domain.Entities;
using AriaHR.Modules.Requests.Domain.Enums;
using AriaHR.Modules.Requests.Infrastructure.Persistence;
using AriaHR.Modules.Requests.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;
using LeaveTypeEnum = AriaHR.Modules.Requests.Domain.Enums.LeaveType;

namespace AriaHR.Modules.Requests.Tests;

public class LeaveRequestAuthorizationAndOwnershipTests
{
    private RequestsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<RequestsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new RequestsDbContext(options);
    }

    private class DictionaryEmployeeLookupService : IEmployeeLookupService
    {
        private readonly Dictionary<Guid, (Guid EmployeeId, Guid OrganizationId)> _mappings;

        public DictionaryEmployeeLookupService(Dictionary<Guid, (Guid EmployeeId, Guid OrganizationId)> mappings)
        {
            _mappings = mappings;
        }

        public Task<(Guid EmployeeId, Guid OrganizationId)?> GetEmployeeDetailsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            if (_mappings.TryGetValue(userId, out var details))
            {
                return Task.FromResult<(Guid, Guid)?>(details);
            }
            return Task.FromResult<(Guid, Guid)?>(null);
        }
    }

    [Fact]
    public async Task GetLeaveRequestById_EmployeeAccessingOtherEmployeeRequest_ThrowsUnauthorizedAccessException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);

        var employeeAUser = Guid.NewGuid();
        var employeeAId = Guid.NewGuid();
        var employeeBUser = Guid.NewGuid();
        var employeeBId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        var lookupService = new DictionaryEmployeeLookupService(new Dictionary<Guid, (Guid, Guid)>
        {
            { employeeAUser, (employeeAId, orgId) },
            { employeeBUser, (employeeBId, orgId) }
        });

        var leaveRequest = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeBId,
            OrganizationId = orgId,
            LeaveType = LeaveTypeEnum.Daily,
            Date = new DateOnly(2026, 10, 5),
            Status = RequestStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
        await repository.AddAsync(leaveRequest);
        await repository.SaveChangesAsync();

        var useCase = new GetLeaveRequestByIdUseCase(repository, lookupService);

        // Employee A tries to access Employee B's request
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            useCase.ExecuteAsync(leaveRequest.Id, employeeAUser, isSystemAdmin: false, isCenterManager: false, userOrganizationId: orgId));
    }

    [Fact]
    public async Task CancelLeaveRequest_EmployeeCancellingOtherEmployeeRequest_ThrowsUnauthorizedAccessException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);

        var employeeAUser = Guid.NewGuid();
        var employeeAId = Guid.NewGuid();
        var employeeBId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        var lookupService = new DictionaryEmployeeLookupService(new Dictionary<Guid, (Guid, Guid)>
        {
            { employeeAUser, (employeeAId, orgId) }
        });

        var leaveRequest = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeBId,
            OrganizationId = orgId,
            LeaveType = LeaveTypeEnum.Daily,
            Date = new DateOnly(2026, 10, 5),
            Status = RequestStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
        await repository.AddAsync(leaveRequest);
        await repository.SaveChangesAsync();

        var useCase = new CancelLeaveRequestUseCase(repository, lookupService);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            useCase.ExecuteAsync(leaveRequest.Id, employeeAUser));
    }

    [Fact]
    public async Task ApproveLeaveRequest_CenterManagerOrgAApprovingOrgBRequest_ThrowsUnauthorizedAccessException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);

        var managerUser = Guid.NewGuid();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        var leaveRequest = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            OrganizationId = orgB, // Belonging to Org B
            LeaveType = LeaveTypeEnum.Daily,
            Date = new DateOnly(2026, 10, 5),
            Status = RequestStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
        await repository.AddAsync(leaveRequest);
        await repository.SaveChangesAsync();

        var useCase = new ApproveLeaveRequestUseCase(repository);

        // Manager of Org A tries to approve Org B's request
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            useCase.ExecuteAsync(leaveRequest.Id, managerUser, userOrganizationId: orgA, isSystemAdmin: false));
    }

    [Fact]
    public async Task RejectLeaveRequest_CenterManagerOrgARejectingOrgBRequest_ThrowsUnauthorizedAccessException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);

        var managerUser = Guid.NewGuid();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        var leaveRequest = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            OrganizationId = orgB,
            LeaveType = LeaveTypeEnum.Daily,
            Date = new DateOnly(2026, 10, 5),
            Status = RequestStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
        await repository.AddAsync(leaveRequest);
        await repository.SaveChangesAsync();

        var useCase = new RejectLeaveRequestUseCase(repository);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            useCase.ExecuteAsync(
                leaveRequest.Id,
                new RejectLeaveRequestRequest { Reason = "Shift conflict" },
                managerUser,
                userOrganizationId: orgA,
                isSystemAdmin: false));
    }
}

using AriaHR.Modules.Requests.Application.DTOs;
using AriaHR.Shared.Services;
using AriaHR.Modules.Requests.Application.UseCases.CreateLeaveRequest;
using AriaHR.Modules.Requests.Domain.Enums;
using AriaHR.Modules.Requests.Infrastructure.Persistence;
using AriaHR.Modules.Requests.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;
using LeaveTypeEnum = AriaHR.Modules.Requests.Domain.Enums.LeaveType;

namespace AriaHR.Modules.Requests.Tests;

public class LeaveRequestValidationTests
{
    private RequestsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<RequestsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new RequestsDbContext(options);
    }

    private class MockEmployeeLookupService : IEmployeeLookupService
    {
        private readonly Guid _employeeId;
        private readonly Guid _organizationId;

        public MockEmployeeLookupService(Guid employeeId, Guid organizationId)
        {
            _employeeId = employeeId;
            _organizationId = organizationId;
        }

        public Task<(Guid EmployeeId, Guid OrganizationId)?> GetEmployeeDetailsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            if (userId == Guid.Empty) return Task.FromResult<(Guid, Guid)?>(null);
            return Task.FromResult<(Guid, Guid)?>((_employeeId, _organizationId));
        }
    }

    [Fact]
    public async Task CreateLeaveRequest_HourlyWithValidData_ReturnsSuccess()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var userId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var lookupService = new MockEmployeeLookupService(employeeId, orgId);

        var useCase = new CreateLeaveRequestUseCase(repository, lookupService);

        var request = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Hourly,
            Date = new DateOnly(2026, 10, 5),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(11, 0),
            Reason = "Medical appointment"
        };

        var result = await useCase.ExecuteAsync(request, userId);

        Assert.NotNull(result);
        Assert.Equal(employeeId, result.EmployeeId);
        Assert.Equal(orgId, result.OrganizationId);
        Assert.Equal(LeaveTypeEnum.Hourly, result.LeaveType);
        Assert.Equal(RequestStatus.Pending, result.Status);
        Assert.Equal(new TimeOnly(9, 0), result.StartTime);
        Assert.Equal(new TimeOnly(11, 0), result.EndTime);
    }

    [Fact]
    public async Task CreateLeaveRequest_DailyWithValidData_IgnoresStartAndEndTime()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var userId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var lookupService = new MockEmployeeLookupService(employeeId, orgId);

        var useCase = new CreateLeaveRequestUseCase(repository, lookupService);

        var request = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Daily,
            Date = new DateOnly(2026, 10, 6),
            StartTime = new TimeOnly(10, 0), // Should be ignored
            EndTime = new TimeOnly(12, 0),   // Should be ignored
            Reason = "Personal leave"
        };

        var result = await useCase.ExecuteAsync(request, userId);

        Assert.NotNull(result);
        Assert.Equal(LeaveTypeEnum.Daily, result.LeaveType);
        Assert.Null(result.StartTime);
        Assert.Null(result.EndTime);
    }

    [Fact]
    public async Task CreateLeaveRequest_HourlyMissingStartTime_ThrowsArgumentException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var userId = Guid.NewGuid();
        var lookupService = new MockEmployeeLookupService(Guid.NewGuid(), Guid.NewGuid());

        var useCase = new CreateLeaveRequestUseCase(repository, lookupService);

        var request = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Hourly,
            Date = new DateOnly(2026, 10, 5),
            StartTime = null,
            EndTime = new TimeOnly(11, 0)
        };

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, userId));
    }

    [Fact]
    public async Task CreateLeaveRequest_HourlyEndTimeBeforeStartTime_ThrowsArgumentException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var userId = Guid.NewGuid();
        var lookupService = new MockEmployeeLookupService(Guid.NewGuid(), Guid.NewGuid());

        var useCase = new CreateLeaveRequestUseCase(repository, lookupService);

        var request = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Hourly,
            Date = new DateOnly(2026, 10, 5),
            StartTime = new TimeOnly(14, 0),
            EndTime = new TimeOnly(12, 0)
        };

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, userId));
    }

    [Fact]
    public async Task CreateLeaveRequest_ReasonExceeds500Chars_ThrowsArgumentException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var userId = Guid.NewGuid();
        var lookupService = new MockEmployeeLookupService(Guid.NewGuid(), Guid.NewGuid());

        var useCase = new CreateLeaveRequestUseCase(repository, lookupService);

        var request = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Daily,
            Date = new DateOnly(2026, 10, 5),
            Reason = new string('A', 501)
        };

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, userId));
    }
}

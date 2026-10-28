using AriaHR.Modules.Attendance.Infrastructure.Persistence;
using AriaHR.Modules.Organization.Domain.Entities;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using AriaHR.Modules.Organization.Infrastructure.UseCases.GetCenterManagerDashboard;
using AriaHR.Modules.Requests.Infrastructure.Persistence;
using AriaHR.Modules.Scheduling.Infrastructure.Persistence;
using AriaHR.Shared.Services;
using AriaHR.Shared.Utils;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AriaHR.Modules.Organization.Tests;

public class DashboardLeaveMetricsTests
{
    private OrganizationDbContext GetOrganizationDbContext()
    {
        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OrganizationDbContext(options);
    }

    private AttendanceDbContext GetAttendanceDbContext()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AttendanceDbContext(options);
    }

    private SchedulingDbContext GetSchedulingDbContext()
    {
        var options = new DbContextOptionsBuilder<SchedulingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new SchedulingDbContext(options);
    }

    private RequestsDbContext GetRequestsDbContext()
    {
        var options = new DbContextOptionsBuilder<RequestsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new RequestsDbContext(options);
    }

    private class FakeCurrentUserService : ICurrentUserService
    {
        public Guid UserId { get; set; } = Guid.NewGuid();
        public Guid? OrganizationId { get; set; } = Guid.NewGuid();
        public IReadOnlyList<string> Roles { get; set; } = new List<string> { "CenterManager" };

        public bool IsInRole(string role) => Roles.Contains(role);

        public Guid ResolveOrganizationId(Guid? requestOrgId) => OrganizationId ?? Guid.Empty;
    }

    private class FakeLeaveLookupService : ILeaveLookupService
    {
        public HashSet<Guid> OnLeaveEmployeeIds { get; set; } = new();

        public Task<bool> IsEmployeeOnApprovedLeaveAsync(Guid employeeId, DateTime utcTimestamp, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(OnLeaveEmployeeIds.Contains(employeeId));
        }

        public Task<HashSet<Guid>> GetEmployeeIdsOnApprovedLeaveTodayAsync(Guid organizationId, DateOnly localDate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(OnLeaveEmployeeIds);
        }

        public Task<Dictionary<Guid, ApprovedLeaveInfoDto>> GetEmployeesApprovedLeaveStatusAsync(Guid organizationId, DateTime utcTimestamp, CancellationToken cancellationToken = default)
        {
            var dict = OnLeaveEmployeeIds.ToDictionary(
                id => id,
                id => new ApprovedLeaveInfoDto(true, "Daily", null, null));
            return Task.FromResult(dict);
        }
    }

    [Fact]
    public async Task GetCenterManagerDashboard_LeaveEmployeesNotCountedAsAbsent()
    {
        // Arrange
        using var orgDb = GetOrganizationDbContext();
        using var attDb = GetAttendanceDbContext();
        using var schedDb = GetSchedulingDbContext();
        using var reqDb = GetRequestsDbContext();

        var currentUser = new FakeCurrentUserService();
        var leaveService = new FakeLeaveLookupService();

        var orgId = currentUser.OrganizationId!.Value;
        var today = TimezoneHelper.GetIranToday();

        // 2 employees
        var emp1 = new Employee { Id = Guid.NewGuid(), OrganizationId = orgId, IsActive = true, IsDeleted = false, PersonnelCode = "E1", NationalCode = "N1", UserId = Guid.NewGuid() };
        var emp2 = new Employee { Id = Guid.NewGuid(), OrganizationId = orgId, IsActive = true, IsDeleted = false, PersonnelCode = "E2", NationalCode = "N2", UserId = Guid.NewGuid() };
        orgDb.Employees.AddRange(emp1, emp2);
        await orgDb.SaveChangesAsync();

        // Both scheduled today
        var shift = new Scheduling.Domain.Entities.Shift { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "Shift", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(16, 0), IsActive = true, IsDeleted = false };
        schedDb.Shifts.Add(shift);
        schedDb.ShiftAssignments.Add(new Scheduling.Domain.Entities.ShiftAssignment { Id = Guid.NewGuid(), OrganizationId = orgId, EmployeeId = emp1.Id, ShiftId = shift.Id, Date = today, IsDeleted = false });
        schedDb.ShiftAssignments.Add(new Scheduling.Domain.Entities.ShiftAssignment { Id = Guid.NewGuid(), OrganizationId = orgId, EmployeeId = emp2.Id, ShiftId = shift.Id, Date = today, IsDeleted = false });
        await schedDb.SaveChangesAsync();

        // emp1 is on approved leave today
        leaveService.OnLeaveEmployeeIds.Add(emp1.Id);

        var useCase = new GetCenterManagerDashboardUseCase(orgDb, attDb, schedDb, reqDb, currentUser, leaveService);

        // Act
        var dashboard = await useCase.ExecuteAsync();

        // Assert
        Assert.Equal(2, dashboard.ActiveEmployeesCount);
        Assert.Equal(1, dashboard.ApprovedLeaveTodayCount);
        Assert.Equal(1, dashboard.AbsentTodayCount); // Only emp2 is absent
    }
}

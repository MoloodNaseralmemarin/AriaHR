using System.Security.Claims;
using AriaHR.Modules.Attendance.Domain.Entities;
using AriaHR.Modules.Attendance.Infrastructure.Persistence;
using AriaHR.Modules.Organization.API.Controllers;
using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Domain.Entities;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using AriaHR.Modules.Organization.Infrastructure.UseCases.GetCenterManagerDashboard;
using AriaHR.Modules.Requests.Domain.Entities;
using AriaHR.Modules.Requests.Domain.Enums;
using AriaHR.Modules.Requests.Infrastructure.Persistence;
using AriaHR.Modules.Scheduling.Domain.Entities;
using AriaHR.Modules.Scheduling.Infrastructure.Persistence;
using AriaHR.Shared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AriaHR.Modules.Organization.Tests;

public class CenterManagerDashboardTests
{
    private OrganizationDbContext GetInMemoryOrganizationDbContext()
    {
        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new OrganizationDbContext(options);
    }

    private AttendanceDbContext GetInMemoryAttendanceDbContext()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AttendanceDbContext(options);
    }

    private SchedulingDbContext GetInMemorySchedulingDbContext()
    {
        var options = new DbContextOptionsBuilder<SchedulingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new SchedulingDbContext(options);
    }

    private RequestsDbContext GetInMemoryRequestsDbContext()
    {
        var options = new DbContextOptionsBuilder<RequestsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RequestsDbContext(options);
    }

    private ICurrentUserService CreateCurrentUserService(Guid userId, string role, Guid? orgId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        if (orgId.HasValue)
        {
            claims.Add(new Claim("organization_id", orgId.Value.ToString()));
        }

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
        };
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        return new CurrentUserService(httpContextAccessor);
    }

    [Fact]
    public async Task ExecuteAsync_CalculatesAllMetricsAccurately()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var managerId = Guid.NewGuid();

        using var orgDb = GetInMemoryOrganizationDbContext();
        using var attDb = GetInMemoryAttendanceDbContext();
        using var schDb = GetInMemorySchedulingDbContext();
        using var reqDb = GetInMemoryRequestsDbContext();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayTimeUtc = DateTime.UtcNow.Date;

        // Active employees
        var emp1 = new Employee { Id = Guid.NewGuid(), OrganizationId = orgId, IsActive = true, IsDeleted = false, PersonnelCode = "P1", NationalCode = "N1" }; // Present, on-time
        var emp2 = new Employee { Id = Guid.NewGuid(), OrganizationId = orgId, IsActive = true, IsDeleted = false, PersonnelCode = "P2", NationalCode = "N2" }; // Present, late
        var emp3 = new Employee { Id = Guid.NewGuid(), OrganizationId = orgId, IsActive = true, IsDeleted = false, PersonnelCode = "P3", NationalCode = "N3" }; // On approved leave
        var emp4 = new Employee { Id = Guid.NewGuid(), OrganizationId = orgId, IsActive = true, IsDeleted = false, PersonnelCode = "P4", NationalCode = "N4" }; // Scheduled, absent
        var empInactive = new Employee { Id = Guid.NewGuid(), OrganizationId = orgId, IsActive = false, IsDeleted = false, PersonnelCode = "P5", NationalCode = "N5" };
        var empDeleted = new Employee { Id = Guid.NewGuid(), OrganizationId = orgId, IsActive = true, IsDeleted = true, PersonnelCode = "P6", NationalCode = "N6" };

        orgDb.Employees.AddRange(emp1, emp2, emp3, emp4, empInactive, empDeleted);
        await orgDb.SaveChangesAsync();

        // Shifts & ShiftAssignments for today
        var shift = new Shift { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "Morning Shift", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(16, 0), IsActive = true };
        schDb.Shifts.Add(shift);

        var sa1 = new ShiftAssignment { Id = Guid.NewGuid(), OrganizationId = orgId, EmployeeId = emp1.Id, ShiftId = shift.Id, Date = today, Shift = shift };
        var sa2 = new ShiftAssignment { Id = Guid.NewGuid(), OrganizationId = orgId, EmployeeId = emp2.Id, ShiftId = shift.Id, Date = today, Shift = shift };
        var sa4 = new ShiftAssignment { Id = Guid.NewGuid(), OrganizationId = orgId, EmployeeId = emp4.Id, ShiftId = shift.Id, Date = today, Shift = shift };
        schDb.ShiftAssignments.AddRange(sa1, sa2, sa4);
        await schDb.SaveChangesAsync();

        // Attendance records for today
        // Emp1 checked in at 07:55 (on-time)
        var att1 = new AttendanceRecord { Id = Guid.NewGuid(), OrganizationId = orgId, EmployeeId = emp1.Id, CheckInTime = todayTimeUtc.AddHours(7).AddMinutes(55) };
        // Emp2 checked in at 08:15 (late) and 08:30 (duplicate record)
        var att2a = new AttendanceRecord { Id = Guid.NewGuid(), OrganizationId = orgId, EmployeeId = emp2.Id, CheckInTime = todayTimeUtc.AddHours(8).AddMinutes(15) };
        var att2b = new AttendanceRecord { Id = Guid.NewGuid(), OrganizationId = orgId, EmployeeId = emp2.Id, CheckInTime = todayTimeUtc.AddHours(8).AddMinutes(30) };

        attDb.AttendanceRecords.AddRange(att1, att2a, att2b);
        await attDb.SaveChangesAsync();

        // Leave request for emp3 today
        var leave3 = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            EmployeeId = emp3.Id,
            Status = RequestStatus.Approved,
            StartDate = today.AddDays(-1),
            EndDate = today.AddDays(1),
            LeaveType = LeaveType.Daily,
            IsDeleted = false
        };
        reqDb.LeaveRequests.Add(leave3);
        await reqDb.SaveChangesAsync();

        var currentUserService = CreateCurrentUserService(managerId, "CenterManager", orgId);
        var useCase = new GetCenterManagerDashboardUseCase(orgDb, attDb, schDb, reqDb, currentUserService);

        // Act
        var result = await useCase.ExecuteAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.ActiveEmployeesCount); // emp1, emp2, emp3, emp4
        Assert.Equal(2, result.PresentTodayCount); // emp1, emp2
        Assert.Equal(1, result.ApprovedLeaveTodayCount); // emp3
        Assert.Equal(1, result.LateArrivalsTodayCount); // emp2
        Assert.Equal(1, result.AbsentTodayCount); // emp4 (scheduled, no attendance, no leave)
    }

    [Fact]
    public async Task ExecuteAsync_EnforcesCrossCenterIsolation()
    {
        // Arrange
        var myOrgId = Guid.NewGuid();
        var otherOrgId = Guid.NewGuid();
        var managerId = Guid.NewGuid();

        using var orgDb = GetInMemoryOrganizationDbContext();
        using var attDb = GetInMemoryAttendanceDbContext();
        using var schDb = GetInMemorySchedulingDbContext();
        using var reqDb = GetInMemoryRequestsDbContext();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var myEmp = new Employee { Id = Guid.NewGuid(), OrganizationId = myOrgId, IsActive = true, IsDeleted = false, PersonnelCode = "M1", NationalCode = "MN1" };
        var otherEmp = new Employee { Id = Guid.NewGuid(), OrganizationId = otherOrgId, IsActive = true, IsDeleted = false, PersonnelCode = "O1", NationalCode = "ON1" };

        orgDb.Employees.AddRange(myEmp, otherEmp);
        await orgDb.SaveChangesAsync();

        // Other center attendance and leave
        var otherAtt = new AttendanceRecord { Id = Guid.NewGuid(), OrganizationId = otherOrgId, EmployeeId = otherEmp.Id, CheckInTime = DateTime.UtcNow };
        attDb.AttendanceRecords.Add(otherAtt);
        await attDb.SaveChangesAsync();

        var otherLeave = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            OrganizationId = otherOrgId,
            EmployeeId = otherEmp.Id,
            Status = RequestStatus.Approved,
            StartDate = today,
            EndDate = today,
            LeaveType = LeaveType.Daily
        };
        reqDb.LeaveRequests.Add(otherLeave);
        await reqDb.SaveChangesAsync();

        var currentUserService = CreateCurrentUserService(managerId, "CenterManager", myOrgId);
        var useCase = new GetCenterManagerDashboardUseCase(orgDb, attDb, schDb, reqDb, currentUserService);

        // Act
        var result = await useCase.ExecuteAsync();

        // Assert
        Assert.Equal(1, result.ActiveEmployeesCount);
        Assert.Equal(0, result.PresentTodayCount);
        Assert.Equal(0, result.ApprovedLeaveTodayCount);
        Assert.Equal(0, result.LateArrivalsTodayCount);
        Assert.Equal(0, result.AbsentTodayCount);
    }

    [Fact]
    public async Task ExecuteAsync_DeduplicatesOverlappingLeaveRequests()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var managerId = Guid.NewGuid();

        using var orgDb = GetInMemoryOrganizationDbContext();
        using var attDb = GetInMemoryAttendanceDbContext();
        using var schDb = GetInMemorySchedulingDbContext();
        using var reqDb = GetInMemoryRequestsDbContext();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var emp1 = new Employee { Id = Guid.NewGuid(), OrganizationId = orgId, IsActive = true, IsDeleted = false, PersonnelCode = "P1", NationalCode = "N1" };
        orgDb.Employees.Add(emp1);
        await orgDb.SaveChangesAsync();

        // Multiple overlapping approved leave requests for same employee
        var leave1 = new LeaveRequest { Id = Guid.NewGuid(), OrganizationId = orgId, EmployeeId = emp1.Id, Status = RequestStatus.Approved, StartDate = today, EndDate = today, LeaveType = LeaveType.Daily };
        var leave2 = new LeaveRequest { Id = Guid.NewGuid(), OrganizationId = orgId, EmployeeId = emp1.Id, Status = RequestStatus.Approved, StartDate = today, EndDate = today, LeaveType = LeaveType.Hourly, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(12, 0) };
        reqDb.LeaveRequests.AddRange(leave1, leave2);
        await reqDb.SaveChangesAsync();

        var currentUserService = CreateCurrentUserService(managerId, "CenterManager", orgId);
        var useCase = new GetCenterManagerDashboardUseCase(orgDb, attDb, schDb, reqDb, currentUserService);

        // Act
        var result = await useCase.ExecuteAsync();

        // Assert
        Assert.Equal(1, result.ApprovedLeaveTodayCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserLacksCenterManagerRole_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        using var orgDb = GetInMemoryOrganizationDbContext();
        using var attDb = GetInMemoryAttendanceDbContext();
        using var schDb = GetInMemorySchedulingDbContext();
        using var reqDb = GetInMemoryRequestsDbContext();

        var currentUserService = CreateCurrentUserService(userId, "Employee", orgId);
        var useCase = new GetCenterManagerDashboardUseCase(orgDb, attDb, schDb, reqDb, currentUserService);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => useCase.ExecuteAsync());
    }

    [Fact]
    public async Task Controller_GetDashboard_ReturnsOkResultForCenterManager()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var managerId = Guid.NewGuid();

        using var orgDb = GetInMemoryOrganizationDbContext();
        using var attDb = GetInMemoryAttendanceDbContext();
        using var schDb = GetInMemorySchedulingDbContext();
        using var reqDb = GetInMemoryRequestsDbContext();

        var currentUserService = CreateCurrentUserService(managerId, "CenterManager", orgId);
        var useCase = new GetCenterManagerDashboardUseCase(orgDb, attDb, schDb, reqDb, currentUserService);
        var controller = new CenterManagerDashboardController(useCase);

        // Act
        var actionResult = await controller.GetDashboard(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var response = Assert.IsType<CenterManagerDashboardResponse>(okResult.Value);
        Assert.Equal(0, response.ActiveEmployeesCount);
    }
}

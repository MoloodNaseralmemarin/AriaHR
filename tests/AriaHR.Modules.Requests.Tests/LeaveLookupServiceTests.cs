using AriaHR.Modules.Requests.Domain.Entities;
using AriaHR.Modules.Requests.Domain.Enums;
using AriaHR.Modules.Requests.Infrastructure.Persistence;
using AriaHR.Modules.Requests.Infrastructure.Services;
using AriaHR.Shared.Utils;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AriaHR.Modules.Requests.Tests;

public class LeaveLookupServiceTests
{
    private RequestsDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<RequestsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RequestsDbContext(options);
    }

    [Fact]
    public async Task IsEmployeeOnApprovedLeave_DailyLeave_ReturnsTrue()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var service = new LeaveLookupService(db);
        var empId = Guid.NewGuid();
        var today = TimezoneHelper.GetIranToday();

        db.LeaveRequests.Add(new LeaveRequest
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            EmployeeId = empId,
            LeaveType = LeaveType.Daily,
            StartDate = today,
            EndDate = today,
            Status = RequestStatus.Approved,
            IsDeleted = false
        });
        await db.SaveChangesAsync();

        // Act & Assert
        Assert.True(await service.IsEmployeeOnApprovedLeaveAsync(empId, DateTime.UtcNow));
    }

    [Fact]
    public async Task IsEmployeeOnApprovedLeave_HourlyLeave_HalfOpenBoundaryEvaluation()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var service = new LeaveLookupService(db);
        var empId = Guid.NewGuid();
        var today = TimezoneHelper.GetIranToday();

        // Approved hourly leave: 10:00 to 12:00
        db.LeaveRequests.Add(new LeaveRequest
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            EmployeeId = empId,
            LeaveType = LeaveType.Hourly,
            StartDate = today,
            EndDate = today,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(12, 0),
            Status = RequestStatus.Approved,
            IsDeleted = false
        });
        await db.SaveChangesAsync();

        var dateVal = today.ToDateTime(TimeOnly.MinValue);
        var beforeLocal = new DateTime(dateVal.Year, dateVal.Month, dateVal.Day, 9, 59, 0);
        var startLocal = new DateTime(dateVal.Year, dateVal.Month, dateVal.Day, 10, 0, 0);
        var insideLocal = new DateTime(dateVal.Year, dateVal.Month, dateVal.Day, 11, 30, 0);
        var endLocal = new DateTime(dateVal.Year, dateVal.Month, dateVal.Day, 12, 0, 0);
        var afterLocal = new DateTime(dateVal.Year, dateVal.Month, dateVal.Day, 12, 1, 0);

        var iranTz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran");
        var beforeUtc = TimeZoneInfo.ConvertTimeToUtc(beforeLocal, iranTz);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, iranTz);
        var insideUtc = TimeZoneInfo.ConvertTimeToUtc(insideLocal, iranTz);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, iranTz);
        var afterUtc = TimeZoneInfo.ConvertTimeToUtc(afterLocal, iranTz);

        // Act & Assert
        Assert.False(await service.IsEmployeeOnApprovedLeaveAsync(empId, beforeUtc));
        Assert.True(await service.IsEmployeeOnApprovedLeaveAsync(empId, startUtc));
        Assert.True(await service.IsEmployeeOnApprovedLeaveAsync(empId, insideUtc));
        Assert.False(await service.IsEmployeeOnApprovedLeaveAsync(empId, endUtc));
        Assert.False(await service.IsEmployeeOnApprovedLeaveAsync(empId, afterUtc));
    }

    [Fact]
    public async Task PendingAndRejectedLeaves_DoNotTriggerRestriction()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var service = new LeaveLookupService(db);
        var empId = Guid.NewGuid();
        var today = TimezoneHelper.GetIranToday();

        db.LeaveRequests.Add(new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = empId, LeaveType = LeaveType.Daily, StartDate = today, EndDate = today, Status = RequestStatus.Pending });
        db.LeaveRequests.Add(new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = empId, LeaveType = LeaveType.Daily, StartDate = today, EndDate = today, Status = RequestStatus.Rejected });
        db.LeaveRequests.Add(new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = empId, LeaveType = LeaveType.Daily, StartDate = today, EndDate = today, Status = RequestStatus.Cancelled });
        await db.SaveChangesAsync();

        // Act & Assert
        Assert.False(await service.IsEmployeeOnApprovedLeaveAsync(empId, DateTime.UtcNow));
    }

    [Fact]
    public async Task GetEmployeeIdsOnApprovedLeaveToday_ReturnsDistinctEmployees()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var service = new LeaveLookupService(db);
        var orgId = Guid.NewGuid();
        var emp1 = Guid.NewGuid();
        var emp2 = Guid.NewGuid();
        var today = TimezoneHelper.GetIranToday();

        // 2 requests for emp1, 1 request for emp2
        db.LeaveRequests.Add(new LeaveRequest { Id = Guid.NewGuid(), OrganizationId = orgId, EmployeeId = emp1, LeaveType = LeaveType.Daily, StartDate = today, EndDate = today, Status = RequestStatus.Approved });
        db.LeaveRequests.Add(new LeaveRequest { Id = Guid.NewGuid(), OrganizationId = orgId, EmployeeId = emp1, LeaveType = LeaveType.Hourly, StartDate = today, EndDate = today, StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0), Status = RequestStatus.Approved });
        db.LeaveRequests.Add(new LeaveRequest { Id = Guid.NewGuid(), OrganizationId = orgId, EmployeeId = emp2, LeaveType = LeaveType.Daily, StartDate = today, EndDate = today, Status = RequestStatus.Approved });
        await db.SaveChangesAsync();

        // Act
        var result = await service.GetEmployeeIdsOnApprovedLeaveTodayAsync(orgId, today);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(emp1, result);
        Assert.Contains(emp2, result);
    }
}

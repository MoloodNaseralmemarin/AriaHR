using AriaHR.Modules.Attendance.Infrastructure.Persistence;
using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.UseCases.GetCenterManagerDashboard;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using AriaHR.Modules.Requests.Domain.Enums;
using AriaHR.Modules.Requests.Infrastructure.Persistence;
using AriaHR.Modules.Scheduling.Infrastructure.Persistence;
using AriaHR.Shared.Services;
using AriaHR.Shared.Utils;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Organization.Infrastructure.UseCases.GetCenterManagerDashboard;

public class GetCenterManagerDashboardUseCase : ICenterManagerDashboardUseCase
{
    private readonly OrganizationDbContext _organizationDbContext;
    private readonly AttendanceDbContext _attendanceDbContext;
    private readonly SchedulingDbContext _schedulingDbContext;
    private readonly RequestsDbContext _requestsDbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILeaveLookupService _leaveLookupService;

    public GetCenterManagerDashboardUseCase(
        OrganizationDbContext organizationDbContext,
        AttendanceDbContext attendanceDbContext,
        SchedulingDbContext schedulingDbContext,
        RequestsDbContext requestsDbContext,
        ICurrentUserService currentUserService,
        ILeaveLookupService leaveLookupService = null!)
    {
        _organizationDbContext = organizationDbContext ?? throw new ArgumentNullException(nameof(organizationDbContext));
        _attendanceDbContext = attendanceDbContext ?? throw new ArgumentNullException(nameof(attendanceDbContext));
        _schedulingDbContext = schedulingDbContext ?? throw new ArgumentNullException(nameof(schedulingDbContext));
        _requestsDbContext = requestsDbContext ?? throw new ArgumentNullException(nameof(requestsDbContext));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _leaveLookupService = leaveLookupService;
    }

    public async Task<CenterManagerDashboardResponse> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.IsInRole("CenterManager"))
        {
            throw new UnauthorizedAccessException("دسترسی غیرمجاز. فقط مدیران مرکز مجاز به مشاهده این داشبورد هستند.");
        }

        var orgId = _currentUserService.ResolveOrganizationId(null);
        if (orgId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("شناسه سازمان معتبر برای کاربر یافت نشد.");
        }

        var today = TimezoneHelper.GetIranToday();
        var todayStartUtc = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var tomorrowStartUtc = today.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        // 1. Active employees in manager's center
        var activeEmployeeIds = await _organizationDbContext.Employees
            .AsNoTracking()
            .Where(e => !e.IsDeleted && e.IsActive && e.OrganizationId == orgId)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        var activeEmployeesCount = activeEmployeeIds.Count;

        if (activeEmployeesCount == 0)
        {
            return new CenterManagerDashboardResponse
            {
                ActiveEmployeesCount = 0,
                PresentTodayCount = 0,
                AbsentTodayCount = 0,
                ApprovedLeaveTodayCount = 0,
                LateArrivalsTodayCount = 0,
                PendingLeaveRequestsCount = 0,
                PendingAttendanceCorrectionRequestsCount = 0,
                UnapprovedShiftsCount = 0
            };
        }

        // 2. Present Today: Active employees with at least one attendance record today
        var presentEmployeeIds = await _attendanceDbContext.AttendanceRecords
            .AsNoTracking()
            .Where(a => !a.IsDeleted && a.OrganizationId == orgId && activeEmployeeIds.Contains(a.EmployeeId) && a.CheckInTime >= todayStartUtc && a.CheckInTime < tomorrowStartUtc)
            .Select(a => a.EmployeeId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var presentTodayCount = presentEmployeeIds.Count;

        // 3. Approved Leave Today: Active employees with approved leave covering today in Iran local date
        HashSet<Guid> approvedLeaveEmployeeIds;
        if (_leaveLookupService != null)
        {
            var onLeaveIds = await _leaveLookupService.GetEmployeeIdsOnApprovedLeaveTodayAsync(orgId, today, cancellationToken);
            approvedLeaveEmployeeIds = new HashSet<Guid>(onLeaveIds.Where(id => activeEmployeeIds.Contains(id)));
        }
        else
        {
            var rawLeaveIds = await _requestsDbContext.LeaveRequests
                .AsNoTracking()
                .Where(l => !l.IsDeleted && l.OrganizationId == orgId && activeEmployeeIds.Contains(l.EmployeeId) && l.Status == RequestStatus.Approved && l.StartDate <= today && l.EndDate >= today)
                .Select(l => l.EmployeeId)
                .Distinct()
                .ToListAsync(cancellationToken);
            approvedLeaveEmployeeIds = new HashSet<Guid>(rawLeaveIds);
        }

        var approvedLeaveTodayCount = approvedLeaveEmployeeIds.Count;

        // 4. Late Arrivals Today & 5. Absent Today
        // Get shift assignments for today for active employees
        var todayShiftAssignments = await _schedulingDbContext.ShiftAssignments
            .AsNoTracking()
            .Include(sa => sa.Shift)
            .Where(sa => !sa.IsDeleted && sa.OrganizationId == orgId && activeEmployeeIds.Contains(sa.EmployeeId) && sa.Date == today && sa.Shift != null && !sa.Shift.IsDeleted)
            .ToListAsync(cancellationToken);

        var scheduledEmployeeIds = todayShiftAssignments.Select(sa => sa.EmployeeId).Distinct().ToList();

        // Get all attendance records for active employees today
        var todayAttendanceRecords = await _attendanceDbContext.AttendanceRecords
            .AsNoTracking()
            .Where(a => !a.IsDeleted && a.OrganizationId == orgId && activeEmployeeIds.Contains(a.EmployeeId) && a.CheckInTime >= todayStartUtc && a.CheckInTime < tomorrowStartUtc)
            .ToListAsync(cancellationToken);

        var earliestCheckIns = todayAttendanceRecords
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(g => g.Key, g => g.Min(a => a.CheckInTime));

        var lateEmployeeIds = new HashSet<Guid>();
        foreach (var sa in todayShiftAssignments)
        {
            if (earliestCheckIns.TryGetValue(sa.EmployeeId, out var checkInTimeUtc))
            {
                var scheduledStartDateTimeUtc = sa.Date.ToDateTime(sa.Shift!.StartTime, DateTimeKind.Utc);
                if (checkInTimeUtc > scheduledStartDateTimeUtc)
                {
                    lateEmployeeIds.Add(sa.EmployeeId);
                }
            }
        }

        var lateArrivalsTodayCount = lateEmployeeIds.Count;

        // Absent Today: Employees scheduled to work today who have no check-in record today AND are not on approved leave today
        var presentEmployeeSet = new HashSet<Guid>(presentEmployeeIds);
        var approvedLeaveEmployeeSet = new HashSet<Guid>(approvedLeaveEmployeeIds);

        int absentTodayCount = scheduledEmployeeIds
            .Count(empId => !presentEmployeeSet.Contains(empId) && !approvedLeaveEmployeeSet.Contains(empId));

        // 6. Pending Leave Requests: Non-deleted pending leave requests for active center employees
        var pendingLeaveRequestsCount = await _requestsDbContext.LeaveRequests
            .AsNoTracking()
            .Where(l => !l.IsDeleted && l.OrganizationId == orgId && activeEmployeeIds.Contains(l.EmployeeId) && l.Status == RequestStatus.Pending)
            .CountAsync(cancellationToken);

        return new CenterManagerDashboardResponse
        {
            ActiveEmployeesCount = activeEmployeesCount,
            PresentTodayCount = presentTodayCount,
            AbsentTodayCount = absentTodayCount,
            ApprovedLeaveTodayCount = approvedLeaveTodayCount,
            LateArrivalsTodayCount = lateArrivalsTodayCount,
            PendingLeaveRequestsCount = pendingLeaveRequestsCount,
            PendingAttendanceCorrectionRequestsCount = 0,
            UnapprovedShiftsCount = 0
        };
    }
}

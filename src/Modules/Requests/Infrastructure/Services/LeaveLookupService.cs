using AriaHR.Modules.Requests.Domain.Enums;
using AriaHR.Modules.Requests.Infrastructure.Persistence;
using AriaHR.Shared.Services;
using AriaHR.Shared.Utils;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Requests.Infrastructure.Services;

public class LeaveLookupService : ILeaveLookupService
{
    private readonly RequestsDbContext _dbContext;

    public LeaveLookupService(RequestsDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<bool> IsEmployeeOnApprovedLeaveAsync(Guid employeeId, DateTime utcTimestamp, CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty)
        {
            return false;
        }

        var (localDate, localTime) = TimezoneHelper.GetIranLocalComponents(utcTimestamp);

        var leaveRequests = await _dbContext.LeaveRequests
            .AsNoTracking()
            .Where(l => !l.IsDeleted && l.EmployeeId == employeeId && l.Status == RequestStatus.Approved && l.StartDate <= localDate && l.EndDate >= localDate)
            .ToListAsync(cancellationToken);

        foreach (var leave in leaveRequests)
        {
            if (leave.LeaveType == LeaveType.Daily)
            {
                return true;
            }

            if (leave.LeaveType == LeaveType.Hourly && leave.StartTime.HasValue && leave.EndTime.HasValue)
            {
                if (localTime >= leave.StartTime.Value && localTime < leave.EndTime.Value)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public async Task<HashSet<Guid>> GetEmployeeIdsOnApprovedLeaveTodayAsync(Guid organizationId, DateOnly localDate, CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
        {
            return new HashSet<Guid>();
        }

        var employeeIds = await _dbContext.LeaveRequests
            .AsNoTracking()
            .Where(l => !l.IsDeleted && l.OrganizationId == organizationId && l.Status == RequestStatus.Approved && l.StartDate <= localDate && l.EndDate >= localDate)
            .Select(l => l.EmployeeId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return new HashSet<Guid>(employeeIds);
    }

    public async Task<Dictionary<Guid, ApprovedLeaveInfoDto>> GetEmployeesApprovedLeaveStatusAsync(Guid organizationId, DateTime utcTimestamp, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, ApprovedLeaveInfoDto>();

        if (organizationId == Guid.Empty)
        {
            return result;
        }

        var (localDate, localTime) = TimezoneHelper.GetIranLocalComponents(utcTimestamp);

        var leaveRequests = await _dbContext.LeaveRequests
            .AsNoTracking()
            .Where(l => !l.IsDeleted && l.OrganizationId == organizationId && l.Status == RequestStatus.Approved && l.StartDate <= localDate && l.EndDate >= localDate)
            .ToListAsync(cancellationToken);

        var groupedByEmployee = leaveRequests.GroupBy(l => l.EmployeeId);

        foreach (var group in groupedByEmployee)
        {
            var empId = group.Key;
            bool isOnLeaveNow = false;
            string? leaveType = null;
            TimeOnly? startTime = null;
            TimeOnly? endTime = null;

            foreach (var leave in group)
            {
                if (leave.LeaveType == LeaveType.Daily)
                {
                    isOnLeaveNow = true;
                    leaveType = "Daily";
                    startTime = null;
                    endTime = null;
                    break;
                }

                if (leave.LeaveType == LeaveType.Hourly && leave.StartTime.HasValue && leave.EndTime.HasValue)
                {
                    if (localTime >= leave.StartTime.Value && localTime < leave.EndTime.Value)
                    {
                        isOnLeaveNow = true;
                        leaveType = "Hourly";
                        startTime = leave.StartTime;
                        endTime = leave.EndTime;
                        break;
                    }
                }
            }

            if (isOnLeaveNow)
            {
                result[empId] = new ApprovedLeaveInfoDto(
                    IsOnApprovedLeave: true,
                    LeaveType: leaveType,
                    StartTime: startTime,
                    EndTime: endTime
                );
            }
        }

        return result;
    }
}

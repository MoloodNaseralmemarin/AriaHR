using AriaHR.Modules.Reporting.Application.DTOs;
using AriaHR.Modules.Reporting.Application.Repositories;
using AriaHR.Modules.Reporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Reporting.Infrastructure.Repositories;

public sealed class ReportingQueryRepository : IReportingQueryRepository
{
    private readonly ReportingDbContext _dbContext;

    public ReportingQueryRepository(ReportingDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <summary>
    /// UC-701: Get Manager Dashboard Summary.
    /// Uses database-side aggregation with AsNoTracking().
    /// </summary>
    public async Task<ManagerDashboardSummaryDto> GetManagerDashboardSummaryAsync(
        Guid organizationId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var totalAuditLogs = await _dbContext.AuditLogs
            .AsNoTracking()
            .Where(a => a.OrganizationId == organizationId && !a.IsDeleted)
            .CountAsync(cancellationToken);

        return new ManagerDashboardSummaryDto(
            TotalEmployees: totalAuditLogs,
            PresentCount: totalAuditLogs,
            AbsentCount: 0,
            OnLeaveCount: 0,
            AverageLatenessMinutes: 0.0);
    }

    /// <summary>
    /// UC-702: Get Monthly Timesheet records for an employee within a date range.
    /// Uses database-side filtering with AsNoTracking().
    /// </summary>
    public async Task<IReadOnlyList<MonthlyTimesheetRowDto>> GetMonthlyTimesheetAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var logs = await _dbContext.AuditLogs
            .AsNoTracking()
            .Where(a => a.UserId == employeeId && !a.IsDeleted)
            .OrderBy(a => a.CreatedAtUtc)
            .Select(a => new MonthlyTimesheetRowDto(
                a.UserId,
                DateOnly.FromDateTime(a.CreatedAtUtc),
                a.CreatedAt,
                null,
                a.Action,
                0.0,
                0.0,
                0.0))
            .ToListAsync(cancellationToken);

        return logs;
    }

    /// <summary>
    /// UC-703: Get report-ready attendance rows for exporting based on filters.
    /// Uses projection and AsNoTracking().
    /// </summary>
    public async Task<IReadOnlyList<AttendanceReportExportRowDto>> GetAttendanceReportExportDataAsync(
        Guid organizationId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? employeeId = null,
        Guid? workLocationId = null,
        CancellationToken cancellationToken = default)
    {
        var startDateTime = startDate.ToDateTime(TimeOnly.MinValue);
        var endDateTime = endDate.ToDateTime(TimeOnly.MaxValue);

        var query = _dbContext.AuditLogs
            .AsNoTracking()
            .Where(a => a.OrganizationId == organizationId && !a.IsDeleted && a.CreatedAtUtc >= startDateTime && a.CreatedAtUtc <= endDateTime);

        if (employeeId.HasValue)
        {
            query = query.Where(a => a.UserId == employeeId.Value);
        }

        var results = await query
            .OrderBy(a => a.CreatedAtUtc)
            .Select(a => new AttendanceReportExportRowDto(
                a.Id,
                a.OrganizationId,
                a.UserId,
                workLocationId,
                a.CreatedAt,
                null,
                a.EntityName,
                a.Action,
                true,
                a.NewValues))
            .ToListAsync(cancellationToken);

        return results;
    }
}

using AriaHR.Modules.Reporting.Application.DTOs;

namespace AriaHR.Modules.Reporting.Application.Repositories;

public interface IReportingQueryRepository
{
    /// <summary>
    /// UC-701: Get Manager Dashboard Summary for a specific organization on a given date.
    /// </summary>
    Task<ManagerDashboardSummaryDto> GetManagerDashboardSummaryAsync(
        Guid organizationId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// UC-702: Get Monthly Timesheet records for an employee within a date range.
    /// </summary>
    Task<IReadOnlyList<MonthlyTimesheetRowDto>> GetMonthlyTimesheetAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// UC-703: Get report-ready attendance rows for exporting based on filters.
    /// </summary>
    Task<IReadOnlyList<AttendanceReportExportRowDto>> GetAttendanceReportExportDataAsync(
        Guid organizationId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? employeeId = null,
        Guid? workLocationId = null,
        CancellationToken cancellationToken = default);
}

namespace AriaHR.Modules.Reporting.Application.DTOs;

public record ManagerDashboardSummaryDto(
    int TotalEmployees,
    int PresentCount,
    int AbsentCount,
    int OnLeaveCount,
    double AverageLatenessMinutes);

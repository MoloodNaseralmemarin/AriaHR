namespace AriaHR.Modules.Reporting.Application.DTOs;

public record MonthlyTimesheetRowDto(
    Guid EmployeeId,
    DateOnly Date,
    DateTime? CheckInTime,
    DateTime? CheckOutTime,
    string Status,
    double TotalWorkedHours,
    double LatenessMinutes,
    double EarlyLeaveMinutes);

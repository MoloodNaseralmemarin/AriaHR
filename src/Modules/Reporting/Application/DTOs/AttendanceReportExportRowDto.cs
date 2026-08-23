namespace AriaHR.Modules.Reporting.Application.DTOs;

public record AttendanceReportExportRowDto(
    Guid AttendanceRecordId,
    Guid OrganizationId,
    Guid EmployeeId,
    Guid? WorkLocationId,
    DateTime CheckInTime,
    DateTime? CheckOutTime,
    string Method,
    string Status,
    bool IsWithinGeofence,
    string? Notes);

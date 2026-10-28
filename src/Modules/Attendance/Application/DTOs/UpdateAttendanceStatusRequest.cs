namespace AriaHR.Modules.Attendance.Application.DTOs;

public class UpdateAttendanceStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

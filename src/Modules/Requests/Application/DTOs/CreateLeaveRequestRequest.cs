using AriaHR.Modules.Requests.Domain.Enums;

namespace AriaHR.Modules.Requests.Application.DTOs;

public class CreateLeaveRequestRequest
{
    public LeaveType LeaveType { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string? Reason { get; set; }
}

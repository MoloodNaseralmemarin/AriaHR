namespace AriaHR.Modules.Scheduling.Application.DTOs;

public record ShiftDto(
    Guid Id,
    Guid OrganizationId,
    string Name,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsActive);

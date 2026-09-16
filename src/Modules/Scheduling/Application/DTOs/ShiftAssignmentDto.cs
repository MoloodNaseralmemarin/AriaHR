namespace AriaHR.Modules.Scheduling.Application.DTOs;

public record ShiftAssignmentDto(
    Guid Id,
    Guid OrganizationId,
    Guid EmployeeId,
    Guid ShiftId,
    string? ShiftName,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    DateOnly Date,
    string Status,
    Guid CreatedBy);

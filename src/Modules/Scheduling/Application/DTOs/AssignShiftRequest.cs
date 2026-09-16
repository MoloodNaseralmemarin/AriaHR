namespace AriaHR.Modules.Scheduling.Application.DTOs;

public record AssignShiftRequest(
    Guid EmployeeId,
    Guid ShiftId,
    DateOnly Date,
    Guid? OrganizationId = null);

namespace AriaHR.Modules.Scheduling.Application.DTOs;

public record DefineShiftRequest(
    string Name,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsActive = true,
    Guid? OrganizationId = null);

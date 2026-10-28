using AriaHR.Modules.Attendance.Application.DTOs;

namespace AriaHR.Modules.Attendance.Application.UseCases;

public interface IGetOrganizationAttendanceUseCase
{
    Task<IReadOnlyList<AttendanceRecordDto>> ExecuteAsync(
        Guid? organizationId = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        Guid? employeeId = null,
        CancellationToken cancellationToken = default);
}

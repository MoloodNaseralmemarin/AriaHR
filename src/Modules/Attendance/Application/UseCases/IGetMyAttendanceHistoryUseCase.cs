using AriaHR.Modules.Attendance.Application.DTOs;

namespace AriaHR.Modules.Attendance.Application.UseCases;

public interface IGetMyAttendanceHistoryUseCase
{
    Task<IReadOnlyList<AttendanceRecordDto>> ExecuteAsync(DateOnly? startDate = null, DateOnly? endDate = null, CancellationToken cancellationToken = default);
}

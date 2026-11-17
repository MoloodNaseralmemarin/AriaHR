using AriaHR.Modules.Attendance.Application.DTOs;

namespace AriaHR.Modules.Attendance.Application.UseCases;

public interface IGetMyAttendanceHistoryUseCase
{
    Task<PagedResult<AttendanceRecordDto>> ExecuteAsync(MyAttendanceHistoryQuery query, CancellationToken cancellationToken = default);
}

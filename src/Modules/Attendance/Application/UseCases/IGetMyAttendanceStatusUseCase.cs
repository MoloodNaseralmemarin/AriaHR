using AriaHR.Modules.Attendance.Application.DTOs;

namespace AriaHR.Modules.Attendance.Application.UseCases;

public interface IGetMyAttendanceStatusUseCase
{
    Task<AttendanceRecordDto?> ExecuteAsync(CancellationToken cancellationToken = default);
}

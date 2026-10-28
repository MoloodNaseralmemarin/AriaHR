using AriaHR.Modules.Attendance.Application.DTOs;

namespace AriaHR.Modules.Attendance.Application.UseCases;

public interface IUpdateAttendanceStatusUseCase
{
    Task<AttendanceRecordDto> ExecuteAsync(Guid recordId, UpdateAttendanceStatusRequest request, CancellationToken cancellationToken = default);
}

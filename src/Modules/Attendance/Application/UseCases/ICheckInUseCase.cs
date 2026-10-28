using AriaHR.Modules.Attendance.Application.DTOs;

namespace AriaHR.Modules.Attendance.Application.UseCases;

public interface ICheckInUseCase
{
    Task<AttendanceRecordDto> ExecuteAsync(CheckInRequest request, CancellationToken cancellationToken = default);
}

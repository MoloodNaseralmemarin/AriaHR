using AriaHR.Modules.Attendance.Application.DTOs;

namespace AriaHR.Modules.Attendance.Application.UseCases;

public interface ICheckOutUseCase
{
    Task<AttendanceRecordDto> ExecuteAsync(CheckOutRequest request, CancellationToken cancellationToken = default);
}

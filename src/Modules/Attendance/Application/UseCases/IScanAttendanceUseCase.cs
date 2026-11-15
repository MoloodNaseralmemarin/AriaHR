using AriaHR.Modules.Attendance.Application.DTOs;

namespace AriaHR.Modules.Attendance.Application.UseCases;

public interface IScanAttendanceUseCase
{
    Task<AttendanceScanResponse> ExecuteAsync(ScanAttendanceRequest request, CancellationToken cancellationToken = default);
}

using AriaHR.Modules.Attendance.Application.DTOs;
using AriaHR.Modules.Attendance.Application.Repositories;
using AriaHR.Shared.Services;

namespace AriaHR.Modules.Attendance.Application.UseCases;

public class GetMyAttendanceHistoryUseCase : IGetMyAttendanceHistoryUseCase
{
    private readonly IAttendanceRecordRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmployeeLookupService _employeeLookupService;

    public GetMyAttendanceHistoryUseCase(
        IAttendanceRecordRepository repository,
        ICurrentUserService currentUserService,
        IEmployeeLookupService employeeLookupService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _employeeLookupService = employeeLookupService ?? throw new ArgumentNullException(nameof(employeeLookupService));
    }

    public async Task<IReadOnlyList<AttendanceRecordDto>> ExecuteAsync(
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("کاربر احراز هویت نشده است.");
        }

        var employeeDetails = await _employeeLookupService.GetEmployeeDetailsByUserIdAsync(userId, cancellationToken);
        if (!employeeDetails.HasValue)
        {
            return Array.Empty<AttendanceRecordDto>();
        }

        var (employeeId, _) = employeeDetails.Value;

        var records = await _repository.GetByEmployeeIdAsync(employeeId, startDate, endDate, cancellationToken);
        if (records.Count == 0)
        {
            return Array.Empty<AttendanceRecordDto>();
        }

        var names = await _employeeLookupService.GetEmployeeNamesByIdsAsync(new[] { employeeId }, cancellationToken);
        string? fullName = names.TryGetValue(employeeId, out var nameTuple) ? nameTuple.FullName : null;

        return records.Select(r => CheckInUseCase.MapToDto(r, fullName)).ToList();
    }
}

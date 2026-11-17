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

    public async Task<PagedResult<AttendanceRecordDto>> ExecuteAsync(
        MyAttendanceHistoryQuery query,
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
            return new PagedResult<AttendanceRecordDto>
            {
                Items = new List<AttendanceRecordDto>(),
                Total = 0,
                TotalPages = 0
            };
        }

        var (employeeId, _) = employeeDetails.Value;

        if (query.Page < 1)
        {
            query.Page = 1;
        }

        if (query.PageSize < 1 || query.PageSize > 100)
        {
            query.PageSize = 20;
        }

        var startDate = query.StartDate?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endDate = query.EndDate.HasValue
            ? query.EndDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
            : null;

        var totalCount = await _repository.GetByEmployeeIdCountAsync(employeeId, startDate, endDate, cancellationToken);
        var records = await _repository.GetByEmployeeIdPagedAsync(employeeId, query.Page, query.PageSize, startDate, endDate, cancellationToken);

        var names = await _employeeLookupService.GetEmployeeNamesByIdsAsync(new[] { employeeId }, cancellationToken);
        string? fullName = names.TryGetValue(employeeId, out var nameTuple) ? nameTuple.FullName : null;

        var dtos = records.Select(r => CheckInUseCase.MapToDto(r, fullName)).ToList();

        return new PagedResult<AttendanceRecordDto>
        {
            Items = dtos,
            Total = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize),
            Page = query.Page
        };
    }
}

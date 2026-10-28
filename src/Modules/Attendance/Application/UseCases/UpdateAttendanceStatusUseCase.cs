using AriaHR.Modules.Attendance.Application.DTOs;
using AriaHR.Modules.Attendance.Application.Repositories;
using AriaHR.Shared.Services;

namespace AriaHR.Modules.Attendance.Application.UseCases;

public class UpdateAttendanceStatusUseCase : IUpdateAttendanceStatusUseCase
{
    private readonly IAttendanceRecordRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmployeeLookupService _employeeLookupService;

    public UpdateAttendanceStatusUseCase(
        IAttendanceRecordRepository repository,
        ICurrentUserService currentUserService,
        IEmployeeLookupService employeeLookupService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _employeeLookupService = employeeLookupService ?? throw new ArgumentNullException(nameof(employeeLookupService));
    }

    public async Task<AttendanceRecordDto> ExecuteAsync(
        Guid recordId,
        UpdateAttendanceStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (recordId == Guid.Empty)
        {
            throw new ArgumentException("شناسه رکورد تردد الزامی است.");
        }

        if (request == null || string.IsNullOrWhiteSpace(request.Status))
        {
            throw new ArgumentException("وضعیت تردد الزامی است.");
        }

        if (!_currentUserService.IsInRole("CenterManager") && !_currentUserService.IsInRole("SystemAdmin"))
        {
            throw new UnauthorizedAccessException("دسترسی غیرمجاز. فقط مدیران مرکز و مدیران سیستم مجاز به تغییر وضعیت تردد هستند.");
        }

        var record = await _repository.GetByIdAsync(recordId, cancellationToken);
        if (record == null)
        {
            throw new KeyNotFoundException("رکورد تردد مورد نظر یافت نشد.");
        }

        if (_currentUserService.IsInRole("CenterManager"))
        {
            var userOrgId = _currentUserService.ResolveOrganizationId(null);
            if (userOrgId == Guid.Empty || record.OrganizationId != userOrgId)
            {
                throw new UnauthorizedAccessException("عدم دسترسی به ویرایش تردد این سازمان.");
            }
        }

        record.Status = request.Status.Trim();
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            record.Notes = string.IsNullOrWhiteSpace(record.Notes)
                ? request.Notes
                : $"{record.Notes}\nAdmin Note: {request.Notes}";
        }

        record.ApprovedBy = _currentUserService.UserId;
        record.ApprovedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(record, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var names = await _employeeLookupService.GetEmployeeNamesByIdsAsync(new[] { record.EmployeeId }, cancellationToken);
        string? fullName = names.TryGetValue(record.EmployeeId, out var nameTuple) ? nameTuple.FullName : null;

        return CheckInUseCase.MapToDto(record, fullName);
    }
}

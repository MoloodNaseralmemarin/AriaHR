using AriaHR.Modules.Attendance.Application.DTOs;
using AriaHR.Modules.Attendance.Application.Repositories;
using AriaHR.Shared.Services;

namespace AriaHR.Modules.Attendance.Application.UseCases;

public class GetOrganizationAttendanceUseCase : IGetOrganizationAttendanceUseCase
{
    private readonly IAttendanceRecordRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmployeeLookupService _employeeLookupService;

    public GetOrganizationAttendanceUseCase(
        IAttendanceRecordRepository repository,
        ICurrentUserService currentUserService,
        IEmployeeLookupService employeeLookupService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _employeeLookupService = employeeLookupService ?? throw new ArgumentNullException(nameof(employeeLookupService));
    }

    public async Task<IReadOnlyList<AttendanceRecordDto>> ExecuteAsync(
        Guid? organizationId = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        Guid? employeeId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.IsInRole("CenterManager") && !_currentUserService.IsInRole("SystemAdmin"))
        {
            throw new UnauthorizedAccessException("دسترسی غیرمجاز. فقط مدیران مرکز و مدیران سیستم مجاز به دریافت گزارش ترددهای سازمان هستند.");
        }

        Guid targetOrgId;

        if (_currentUserService.IsInRole("CenterManager"))
        {
            targetOrgId = _currentUserService.ResolveOrganizationId(organizationId);
            if (targetOrgId == Guid.Empty)
            {
                throw new UnauthorizedAccessException("شناسه سازمان معتبر برای کاربر مدیر مرکز یافت نشد.");
            }

            if (organizationId.HasValue && organizationId.Value != Guid.Empty && organizationId.Value != targetOrgId)
            {
                throw new UnauthorizedAccessException("عدم دسترسی به اطلاعات تردد سازمان دیگر.");
            }
        }
        else
        {
            // SystemAdmin
            if (organizationId.HasValue && organizationId.Value != Guid.Empty)
            {
                targetOrgId = organizationId.Value;
            }
            else if (_currentUserService.OrganizationId.HasValue && _currentUserService.OrganizationId.Value != Guid.Empty)
            {
                targetOrgId = _currentUserService.OrganizationId.Value;
            }
            else
            {
                throw new ArgumentException("شناسه سازمان برای استعلام ترددهای سازمان الزامی است.");
            }
        }

        var records = await _repository.GetByOrganizationIdAsync(targetOrgId, startDate, endDate, employeeId, cancellationToken);
        if (records.Count == 0)
        {
            return Array.Empty<AttendanceRecordDto>();
        }

        var empIds = records.Select(r => r.EmployeeId).Distinct();
        var nameDict = await _employeeLookupService.GetEmployeeNamesByIdsAsync(empIds, cancellationToken);

        return records.Select(r =>
        {
            string? fullName = nameDict.TryGetValue(r.EmployeeId, out var nameTuple) ? nameTuple.FullName : null;
            return CheckInUseCase.MapToDto(r, fullName);
        }).ToList();
    }
}

using AriaHR.Modules.Attendance.Application.DTOs;
using AriaHR.Modules.Attendance.Application.Repositories;
using AriaHR.Shared.Services;

namespace AriaHR.Modules.Attendance.Application.UseCases;

public class CheckOutUseCase : ICheckOutUseCase
{
    private readonly IAttendanceRecordRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmployeeLookupService _employeeLookupService;

    public CheckOutUseCase(
        IAttendanceRecordRepository repository,
        ICurrentUserService currentUserService,
        IEmployeeLookupService employeeLookupService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _employeeLookupService = employeeLookupService ?? throw new ArgumentNullException(nameof(employeeLookupService));
    }

    public async Task<AttendanceRecordDto> ExecuteAsync(CheckOutRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var userId = _currentUserService.UserId;
        if (userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("کاربر احراز هویت نشده است.");
        }

        var employeeDetails = await _employeeLookupService.GetEmployeeDetailsByUserIdAsync(userId, cancellationToken);
        if (!employeeDetails.HasValue)
        {
            throw new InvalidOperationException("اطلاعات کارمندی برای کاربر جاری یافت نشد.");
        }

        var (employeeId, _) = employeeDetails.Value;

        var activeRecord = await _repository.GetActiveCheckInByEmployeeIdAsync(employeeId, cancellationToken);
        if (activeRecord == null)
        {
            throw new InvalidOperationException("تردد ورودی فعال برای خروج یافت نشد.");
        }

        activeRecord.CheckOutTime = DateTime.UtcNow;
        if (request.Latitude.HasValue) activeRecord.CheckOutLatitude = request.Latitude;
        if (request.Longitude.HasValue) activeRecord.CheckOutLongitude = request.Longitude;
        if (request.Accuracy.HasValue) activeRecord.CheckOutAccuracy = request.Accuracy;

        if (!string.IsNullOrWhiteSpace(request.DeviceInfo))
        {
            activeRecord.DeviceInfo = string.IsNullOrWhiteSpace(activeRecord.DeviceInfo)
                ? request.DeviceInfo
                : $"{activeRecord.DeviceInfo} | Checkout: {request.DeviceInfo}";
        }

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            activeRecord.Notes = string.IsNullOrWhiteSpace(activeRecord.Notes)
                ? request.Notes
                : $"{activeRecord.Notes}\nCheckout Note: {request.Notes}";
        }

        if (activeRecord.Status != "Approved")
        {
            activeRecord.Status = "Completed";
        }

        await _repository.UpdateAsync(activeRecord, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var names = await _employeeLookupService.GetEmployeeNamesByIdsAsync(new[] { employeeId }, cancellationToken);
        string? fullName = names.TryGetValue(employeeId, out var nameTuple) ? nameTuple.FullName : null;

        return CheckInUseCase.MapToDto(activeRecord, fullName);
    }
}

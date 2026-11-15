using AriaHR.Modules.Attendance.Application.DTOs;
using AriaHR.Modules.Attendance.Application.Exceptions;
using AriaHR.Modules.Attendance.Application.Repositories;
using AriaHR.Modules.Attendance.Domain.Entities;
using AriaHR.Shared.Services;

namespace AriaHR.Modules.Attendance.Application.UseCases;

public class CheckInUseCase : ICheckInUseCase
{
    private readonly IAttendanceRecordRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmployeeLookupService _employeeLookupService;
    private readonly IWorkLocationLookupService _workLocationLookupService;
    private readonly ILeaveLookupService? _leaveLookupService;

    public CheckInUseCase(
        IAttendanceRecordRepository repository,
        ICurrentUserService currentUserService,
        IEmployeeLookupService employeeLookupService,
        IWorkLocationLookupService workLocationLookupService,
        ILeaveLookupService? leaveLookupService = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _employeeLookupService = employeeLookupService ?? throw new ArgumentNullException(nameof(employeeLookupService));
        _workLocationLookupService = workLocationLookupService ?? throw new ArgumentNullException(nameof(workLocationLookupService));
        _leaveLookupService = leaveLookupService;
    }

    public async Task<AttendanceRecordDto> ExecuteAsync(CheckInRequest request, CancellationToken cancellationToken = default)
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

        var (employeeId, organizationId) = employeeDetails.Value;

        var nowUtc = DateTime.UtcNow;

        if (_leaveLookupService != null && await _leaveLookupService.IsEmployeeOnApprovedLeaveAsync(employeeId, nowUtc, cancellationToken))
        {
            throw new LeaveRestrictionException();
        }

        var activeCheckIn = await _repository.GetActiveCheckInByEmployeeIdAsync(employeeId, cancellationToken);
        if (activeCheckIn != null)
        {
            throw new InvalidOperationException("کاربر دارای یک ورود فعال است و باید ابتدا خروج ثبت کند.");
        }

        Guid? workLocationId = request.WorkLocationId;
        Guid? qrCodeId = null;
        bool isWithinGeofence = false;
        string method = string.IsNullOrWhiteSpace(request.Method) ? "GPS" : request.Method.Trim();

        if (!string.IsNullOrWhiteSpace(request.QRCodeToken) || string.Equals(method, "QRCode", StringComparison.OrdinalIgnoreCase))
        {
            method = "QRCode";
            var qrResult = await _workLocationLookupService.ValidateQrCodeAsync(request.QRCodeToken ?? string.Empty, organizationId, null, cancellationToken);
            if (!qrResult.IsValid)
            {
                throw new ArgumentException(qrResult.ErrorMessage ?? "کد QR نامعتبر است.");
            }

            qrCodeId = qrResult.QRCodeId;
            workLocationId = qrResult.WorkLocationId;

            // The legacy check-in endpoint keeps working, but it must not claim physical presence
            // without a server-side geofence evaluation. The canonical flow is POST /api/attendance/scan.
            if (request.Latitude.HasValue && request.Longitude.HasValue && workLocationId.HasValue)
            {
                var qrWorkLocation = await _workLocationLookupService.GetWorkLocationByIdAsync(workLocationId.Value, cancellationToken);
                if (qrWorkLocation != null && qrWorkLocation.IsActive)
                {
                    double qrDistance = CalculateDistanceInMeters(
                        request.Latitude.Value,
                        request.Longitude.Value,
                        (double)qrWorkLocation.Latitude,
                        (double)qrWorkLocation.Longitude);

                    isWithinGeofence = qrDistance <= (double)qrWorkLocation.AllowedRadiusInMeters;
                }
            }
        }
        else if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            var workLocation = workLocationId.HasValue
                ? await _workLocationLookupService.GetWorkLocationByIdAsync(workLocationId.Value, cancellationToken)
                : await _workLocationLookupService.GetWorkLocationForOrganizationAsync(organizationId, cancellationToken);

            if (workLocation != null && workLocation.IsActive)
            {
                workLocationId = workLocation.Id;
                double distance = CalculateDistanceInMeters(
                    request.Latitude.Value,
                    request.Longitude.Value,
                    (double)workLocation.Latitude,
                    (double)workLocation.Longitude);

                if (distance <= (double)workLocation.AllowedRadiusInMeters)
                {
                    isWithinGeofence = true;
                }
            }
        }

        var record = new AttendanceRecord
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            EmployeeId = employeeId,
            CheckInTime = nowUtc,
            CheckInLatitude = request.Latitude,
            CheckInLongitude = request.Longitude,
            CheckInAccuracy = request.Accuracy,
            WorkLocationId = workLocationId,
            QRCodeId = qrCodeId,
            Method = method,
            Status = "Present",
            IsWithinGeofence = isWithinGeofence,
            DeviceInfo = request.DeviceInfo,
            IPAddress = request.IPAddress,
            Notes = request.Notes,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _repository.AddAsync(record, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var names = await _employeeLookupService.GetEmployeeNamesByIdsAsync(new[] { employeeId }, cancellationToken);
        string? fullName = names.TryGetValue(employeeId, out var nameTuple) ? nameTuple.FullName : null;

        return MapToDto(record, fullName);
    }

    private static double CalculateDistanceInMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double deg) => deg * (Math.PI / 180.0);

    internal static AttendanceRecordDto MapToDto(AttendanceRecord r, string? employeeFullName = null)
    {
        return new AttendanceRecordDto
        {
            Id = r.Id,
            OrganizationId = r.OrganizationId,
            EmployeeId = r.EmployeeId,
            EmployeeFullName = employeeFullName,
            CheckInTime = r.CheckInTime,
            CheckOutTime = r.CheckOutTime,
            CheckInLatitude = r.CheckInLatitude,
            CheckInLongitude = r.CheckInLongitude,
            CheckOutLatitude = r.CheckOutLatitude,
            CheckOutLongitude = r.CheckOutLongitude,
            CheckInAccuracy = r.CheckInAccuracy,
            CheckOutAccuracy = r.CheckOutAccuracy,
            CheckInDistanceInMeters = r.CheckInDistanceInMeters,
            CheckOutDistanceInMeters = r.CheckOutDistanceInMeters,
            CheckInLocationTimestampUtc = r.CheckInLocationTimestampUtc,
            CheckOutLocationTimestampUtc = r.CheckOutLocationTimestampUtc,
            WorkLocationId = r.WorkLocationId,
            QRCodeId = r.QRCodeId,
            Method = r.Method,
            Status = r.Status,
            IsWithinGeofence = r.IsWithinGeofence,
            DeviceInfo = r.DeviceInfo,
            IPAddress = r.IPAddress,
            Notes = r.Notes,
            ApprovedBy = r.ApprovedBy,
            ApprovedAt = r.ApprovedAt,
            CreatedAtUtc = r.CreatedAtUtc
        };
    }
}

using AriaHR.Modules.Attendance.Application.DTOs;
using AriaHR.Modules.Attendance.Application.Exceptions;
using AriaHR.Modules.Attendance.Application.Geofencing;
using AriaHR.Modules.Attendance.Application.Options;
using AriaHR.Modules.Attendance.Application.Repositories;
using AriaHR.Modules.Attendance.Application.Shifts;
using AriaHR.Modules.Attendance.Domain.Entities;
using AriaHR.Shared.Services;
using AriaHR.Shared.Utils;
using Microsoft.Extensions.Options;

namespace AriaHR.Modules.Attendance.Application.UseCases;

/// <summary>
/// QR + GPS attendance scan.
/// The employee identity comes from JWT claims; the work location coordinates, allowed radius and shift rules
/// are always read from the database and every validation decision is made on the server.
/// </summary>
public class ScanAttendanceUseCase : IScanAttendanceUseCase
{
    private const string CheckInAction = "CheckIn";
    private const string CheckOutAction = "CheckOut";

    private readonly IAttendanceRecordRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmployeeLookupService _employeeLookupService;
    private readonly IWorkLocationLookupService _workLocationLookupService;
    private readonly GpsValidationOptions _gpsOptions;
    private readonly AttendancePolicyOptions _policyOptions;
    private readonly IShiftLookupService? _shiftLookupService;
    private readonly ILeaveLookupService? _leaveLookupService;

    public ScanAttendanceUseCase(
        IAttendanceRecordRepository repository,
        ICurrentUserService currentUserService,
        IEmployeeLookupService employeeLookupService,
        IWorkLocationLookupService workLocationLookupService,
        IOptions<GpsValidationOptions> gpsOptions,
        IOptions<AttendancePolicyOptions> policyOptions,
        IShiftLookupService? shiftLookupService = null,
        ILeaveLookupService? leaveLookupService = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _employeeLookupService = employeeLookupService ?? throw new ArgumentNullException(nameof(employeeLookupService));
        _workLocationLookupService = workLocationLookupService ?? throw new ArgumentNullException(nameof(workLocationLookupService));
        _gpsOptions = gpsOptions?.Value ?? new GpsValidationOptions();
        _policyOptions = policyOptions?.Value ?? new AttendancePolicyOptions();
        _shiftLookupService = shiftLookupService;
        _leaveLookupService = leaveLookupService;
    }

    public async Task<AttendanceScanResponse> ExecuteAsync(
        ScanAttendanceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        Guid userId = _currentUserService.UserId;
        if (userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("کاربر احراز هویت نشده است.");
        }

        var employeeDetails = await _employeeLookupService.GetEmployeeDetailsByUserIdAsync(userId, cancellationToken);
        if (!employeeDetails.HasValue)
        {
            throw new AttendanceScanException(
                AttendanceErrorCodes.EmployeeNotFound,
                "اطلاعات کارمندی برای کاربر جاری یافت نشد.",
                403);
        }

        var (employeeId, organizationId) = employeeDetails.Value;
        DateTime nowUtc = DateTime.UtcNow;

        string? idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? null
            : request.IdempotencyKey.Trim();

        if (idempotencyKey != null && idempotencyKey.Length > 100)
        {
            throw new AttendanceScanException(
                AttendanceErrorCodes.InvalidRequest,
                "شناسه یکتای درخواست نمی‌تواند بیش از ۱۰۰ کاراکتر باشد.",
                400);
        }

        if (idempotencyKey != null)
        {
            var replayed = await _repository.GetByEmployeeAndIdempotencyKeyAsync(employeeId, idempotencyKey, cancellationToken);
            if (replayed != null)
            {
                bool isCheckOutReplay = string.Equals(replayed.CheckOutIdempotencyKey, idempotencyKey, StringComparison.Ordinal);
                return await BuildReplayResponseAsync(replayed, isCheckOutReplay ? CheckOutAction : CheckInAction, cancellationToken);
            }
        }

        if (request.DeviceInfo != null && request.DeviceInfo.Length > 500)
        {
            throw new AttendanceScanException(
                AttendanceErrorCodes.InvalidRequest,
                "اطلاعات دستگاه نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد.",
                400);
        }

        if (request.Notes != null && request.Notes.Length > 1000)
        {
            throw new AttendanceScanException(
                AttendanceErrorCodes.InvalidRequest,
                "توضیحات نمی‌تواند بیشتر از ۱۰۰۰ کاراکتر باشد.",
                400);
        }

        // 1) QR authenticity, enabled state, mode and expiration.
        var qrValidation = await _workLocationLookupService.ValidateQrCodeAsync(
            request.QRCodeToken ?? string.Empty,
            organizationId,
            nowUtc,
            cancellationToken);

        if (!qrValidation.IsValid)
        {
            string code = string.IsNullOrWhiteSpace(qrValidation.ErrorCode)
                ? AttendanceErrorCodes.QrCodeInvalid
                : qrValidation.ErrorCode!;

            int status = code switch
            {
                AttendanceErrorCodes.EmployeeNotAuthorized => 403,
                AttendanceErrorCodes.WorkLocationNotFound => 404,
                _ => 400
            };

            throw new AttendanceScanException(
                code,
                qrValidation.ErrorMessage ?? "کد کیوآر معتبر نمی‌باشد.",
                status);
        }

        if (!qrValidation.WorkLocationId.HasValue || qrValidation.WorkLocationId.Value == Guid.Empty)
        {
            throw new AttendanceScanException(
                AttendanceErrorCodes.WorkLocationNotFound,
                "محل کار مرتبط با کد کیوآر یافت نشد.",
                404);
        }

        // 2) Official work location configuration.
        var workLocation = await _workLocationLookupService.GetWorkLocationByIdAsync(
            qrValidation.WorkLocationId.Value,
            cancellationToken);

        if (workLocation == null)
        {
            throw new AttendanceScanException(
                AttendanceErrorCodes.WorkLocationNotFound,
                "محل کار مرتبط با کد کیوآر یافت نشد.",
                404);
        }

        if (!workLocation.IsActive)
        {
            throw new AttendanceScanException(
                AttendanceErrorCodes.WorkLocationInactive,
                "محل کار مورد نظر غیرفعال است.",
                400,
                BuildLocationData(workLocation.Id, workLocation.Name, workLocation.AllowedRadiusInMeters));
        }

        // 3) Employee must belong to the work location's center/organization.
        if (workLocation.OrganizationId != organizationId)
        {
            throw new AttendanceScanException(
                AttendanceErrorCodes.EmployeeNotAuthorized,
                "شما مجاز به ثبت تردد در این محل کار نیستید.",
                403);
        }

        // 4) GPS validation (missing, ranges, accuracy, freshness).
        var gps = ValidateGps(request, nowUtc, workLocation);

        // 5) Server-side geofence evaluation.
        double distanceMeters = GeoDistanceCalculator.CalculateDistanceInMeters(
            gps.Latitude,
            gps.Longitude,
            (double)workLocation.Latitude,
            (double)workLocation.Longitude);

        double allowedRadius = (double)workLocation.AllowedRadiusInMeters;
        if (distanceMeters > allowedRadius)
        {
            var data = BuildLocationData(
                workLocation.Id,
                workLocation.Name,
                workLocation.AllowedRadiusInMeters,
                distanceMeters,
                request.GpsAccuracyInMeters);

            throw new AttendanceScanException(
                AttendanceErrorCodes.OutsideAllowedRadius,
                "شما خارج از محدوده مجاز محل کار هستید.",
                400,
                data);
        }

        var activeRecord = await _repository.GetActiveCheckInByEmployeeIdAsync(employeeId, cancellationToken);
        string attendanceAction = ResolveAction(request.AttendanceAction, activeRecord != null);

        if (attendanceAction == CheckInAction && activeRecord != null)
        {
            throw new AttendanceScanException(
                AttendanceErrorCodes.DuplicateAttendance,
                "برای امروز ورود فعال ثبت شده است. ابتدا خروج ثبت کنید.",
                409);
        }

        if (attendanceAction == CheckOutAction && activeRecord == null)
        {
            throw new AttendanceScanException(
                AttendanceErrorCodes.InvalidAttendanceAction,
                "تردد ورودی فعالی برای ثبت خروج یافت نشد.",
                400);
        }

        if (_leaveLookupService != null &&
            await _leaveLookupService.IsEmployeeOnApprovedLeaveAsync(employeeId, nowUtc, cancellationToken))
        {
            throw new LeaveRestrictionException();
        }

        // 6) Shift assignment and working window.
        var shiftEvaluation = await ValidateShiftAsync(employeeId, attendanceAction, nowUtc, cancellationToken);

        // 7) Persist only after every validation passed.
        if (attendanceAction == CheckInAction)
        {
            var record = new AttendanceRecord
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                EmployeeId = employeeId,
                CheckInTime = nowUtc,
                CheckInLatitude = (double)request.Latitude!.Value,
                CheckInLongitude = (double)request.Longitude!.Value,
                CheckInAccuracy = request.GpsAccuracyInMeters,
                CheckInDistanceInMeters = distanceMeters,
                CheckInLocationTimestampUtc = gps.LocationTimestampUtc,
                CheckInIdempotencyKey = idempotencyKey,
                WorkLocationId = workLocation.Id,
                QRCodeId = qrValidation.QRCodeId,
                Method = "QRCode",
                Status = "Present",
                IsWithinGeofence = true,
                DeviceInfo = request.DeviceInfo,
                Notes = request.Notes,
                CreatedAtUtc = nowUtc
            };

            await _repository.AddAsync(record, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            return AttendanceScanResponse.Ok(
                AttendanceErrorCodes.AttendanceRegistered,
                "حضور شما با موفقیت ثبت شد",
                BuildData(record, workLocation, CheckInAction, distanceMeters, shiftEvaluation.IsLate, shiftEvaluation.IsEarlyDeparture, isReplay: false, shiftName: shiftEvaluation.ShiftName));
        }

        var active = activeRecord!;
        active.CheckOutTime = nowUtc;
        active.CheckOutLatitude = (double)request.Latitude!.Value;
        active.CheckOutLongitude = (double)request.Longitude!.Value;
        active.CheckOutAccuracy = request.GpsAccuracyInMeters;
        active.CheckOutDistanceInMeters = distanceMeters;
        active.CheckOutLocationTimestampUtc = gps.LocationTimestampUtc;
        active.CheckOutIdempotencyKey = idempotencyKey;
        active.WorkLocationId ??= workLocation.Id;

        if (!string.IsNullOrWhiteSpace(request.DeviceInfo))
        {
            active.DeviceInfo = string.IsNullOrWhiteSpace(active.DeviceInfo)
                ? request.DeviceInfo
                : $"{active.DeviceInfo} | Checkout: {request.DeviceInfo}";
        }

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            active.Notes = string.IsNullOrWhiteSpace(active.Notes)
                ? request.Notes
                : $"{active.Notes}\nCheckout Note: {request.Notes}";
        }

        if (active.Status != "Approved")
        {
            active.Status = "Completed";
        }

        active.UpdatedAtUtc = nowUtc;

        await _repository.UpdateAsync(active, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return AttendanceScanResponse.Ok(
            AttendanceErrorCodes.AttendanceRegistered,
            "خروج شما با موفقیت ثبت شد",
            BuildData(active, workLocation, CheckOutAction, distanceMeters, isLate: false, isEarlyDeparture: shiftEvaluation.IsEarlyDeparture, isReplay: false, shiftName: shiftEvaluation.ShiftName));
    }

    private GpsReading ValidateGps(
        ScanAttendanceRequest request,
        DateTime nowUtc,
        WorkLocationDetailsDto workLocation)
    {
        if (!request.Latitude.HasValue || !request.Longitude.HasValue)
        {
            throw new AttendanceScanException(
                AttendanceErrorCodes.LocationUnavailable,
                "اطلاعات موقعیت مکانی ارسال نشده است. لطفاً دسترسی به موقعیت مکانی را فعال کنید.",
                400,
                BuildLocationData(workLocation.Id, workLocation.Name, workLocation.AllowedRadiusInMeters));
        }

        double latitude = (double)request.Latitude.Value;
        double longitude = (double)request.Longitude.Value;

        if (!GeoDistanceCalculator.IsValidLatitude(latitude) || !GeoDistanceCalculator.IsValidLongitude(longitude))
        {
            throw new AttendanceScanException(
                AttendanceErrorCodes.LocationCoordinatesInvalid,
                "مختصات جغرافیایی ارسال شده معتبر نمی‌باشد.",
                400,
                BuildLocationData(workLocation.Id, workLocation.Name, workLocation.AllowedRadiusInMeters, gpsAccuracy: request.GpsAccuracyInMeters));
        }

        if (!request.GpsAccuracyInMeters.HasValue ||
            request.GpsAccuracyInMeters.Value <= 0 ||
            request.GpsAccuracyInMeters.Value > _gpsOptions.MaxAccuracyMeters)
        {
            throw new AttendanceScanException(
                AttendanceErrorCodes.LocationAccuracyInsufficient,
                "دقت موقعیت مکانی کافی نیست. لطفاً در فضای باز قرار بگیرید و دوباره تلاش کنید.",
                400,
                BuildLocationData(
                    workLocation.Id,
                    workLocation.Name,
                    workLocation.AllowedRadiusInMeters,
                    gpsAccuracy: request.GpsAccuracyInMeters));
        }

        if (!request.LocationTimestampUtc.HasValue)
        {
            throw new AttendanceScanException(
                AttendanceErrorCodes.LocationTimestampInvalid,
                "زمان ثبت موقعیت مکانی ارسال نشده است.",
                400,
                BuildLocationData(workLocation.Id, workLocation.Name, workLocation.AllowedRadiusInMeters));
        }

        DateTime locationTimestampUtc = NormalizeUtc(request.LocationTimestampUtc.Value);

        if (locationTimestampUtc > nowUtc.AddSeconds(_gpsOptions.MaxFutureSkewSeconds))
        {
            throw new AttendanceScanException(
                AttendanceErrorCodes.LocationTimestampInvalid,
                "زمان موقعیت مکانی نمی‌تواند در آینده باشد.",
                400);
        }

        if (locationTimestampUtc < nowUtc.AddSeconds(-_gpsOptions.MaxLocationAgeSeconds))
        {
            throw new AttendanceScanException(
                AttendanceErrorCodes.LocationDataExpired,
                "اطلاعات موقعیت مکانی قدیمی است. لطفاً موقعیت خود را به‌روزرسانی کنید.",
                400);
        }

        return new GpsReading(latitude, longitude, locationTimestampUtc);
    }

    private async Task<ShiftEvaluation> ValidateShiftAsync(
        Guid employeeId,
        string attendanceAction,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        if (!_policyOptions.ShiftValidationEnabled || _shiftLookupService == null)
        {
            return new ShiftEvaluation(false, false, null);
        }

        var (localDate, localTime) = TimezoneHelper.GetIranLocalComponents(nowUtc);
        var assignment = await _shiftLookupService.GetShiftAssignmentForEmployeeOnDateAsync(
            employeeId,
            localDate,
            cancellationToken);

        if (assignment == null)
        {
            // Employees without an assignment are not blocked; existing attendance behaviour is preserved.
            return new ShiftEvaluation(false, false, null);
        }

        if (!assignment.IsShiftActive)
        {
            throw new AttendanceScanException(
                AttendanceErrorCodes.ShiftValidationFailed,
                $"شیفت «{assignment.ShiftName}» غیرفعال است.",
                400);
        }

        try
        {
            var evaluation = attendanceAction == CheckInAction
                ? ShiftTimeValidator.ValidateCheckIn(assignment, localTime, _policyOptions)
                : ShiftTimeValidator.ValidateCheckOut(assignment, localTime, _policyOptions);

            return new ShiftEvaluation(evaluation.IsLate, evaluation.IsEarlyDeparture, assignment.ShiftName);
        }
        catch (InvalidOperationException ex)
        {
            throw new AttendanceScanException(
                AttendanceErrorCodes.ShiftValidationFailed,
                ex.Message,
                400);
        }
    }

    private async Task<AttendanceScanResponse> BuildReplayResponseAsync(
        AttendanceRecord record,
        string action,
        CancellationToken cancellationToken)
    {
        WorkLocationDetailsDto? workLocation = null;
        if (record.WorkLocationId.HasValue)
        {
            workLocation = await _workLocationLookupService.GetWorkLocationByIdAsync(record.WorkLocationId.Value, cancellationToken);
        }

        var data = new AttendanceScanData
        {
            AttendanceId = record.Id,
            WorkLocationId = record.WorkLocationId,
            WorkLocationName = workLocation?.Name,
            AttendanceAction = action,
            DistanceInMeters = action == CheckInAction
                ? ToMeters(record.CheckInDistanceInMeters)
                : ToMeters(record.CheckOutDistanceInMeters),
            AllowedRadiusInMeters = workLocation?.AllowedRadiusInMeters,
            RegisteredAtUtc = action == CheckInAction ? record.CheckInTime : record.CheckOutTime,
            AttendanceStatus = record.Status,
            IsReplay = true
        };

        return AttendanceScanResponse.Ok(
            AttendanceErrorCodes.AttendanceRegistered,
            "این درخواست قبلاً ثبت شده است و مجدداً ثبت نشد.",
            data);
    }

    private static AttendanceScanData BuildData(
        AttendanceRecord record,
        WorkLocationDetailsDto workLocation,
        string action,
        double distanceMeters,
        bool isLate,
        bool isEarlyDeparture,
        bool isReplay,
        string? shiftName)
    {
        return new AttendanceScanData
        {
            AttendanceId = record.Id,
            WorkLocationId = workLocation.Id,
            WorkLocationName = workLocation.Name,
            AttendanceAction = action,
            DistanceInMeters = ToMeters(distanceMeters),
            AllowedRadiusInMeters = workLocation.AllowedRadiusInMeters,
            RegisteredAtUtc = action == CheckInAction ? record.CheckInTime : record.CheckOutTime,
            AttendanceStatus = record.Status,
            IsLate = isLate,
            IsEarlyDeparture = isEarlyDeparture,
            ShiftName = shiftName,
            IsReplay = isReplay
        };
    }

    private static AttendanceScanData BuildLocationData(
        Guid workLocationId,
        string workLocationName,
        decimal allowedRadiusInMeters,
        double? distanceMeters = null,
        double? gpsAccuracy = null)
    {
        return new AttendanceScanData
        {
            WorkLocationId = workLocationId,
            WorkLocationName = workLocationName,
            AllowedRadiusInMeters = allowedRadiusInMeters,
            DistanceInMeters = distanceMeters.HasValue ? ToMeters(distanceMeters.Value) : null,
            GpsAccuracyInMeters = gpsAccuracy
        };
    }

    private static decimal ToMeters(double? value)
    {
        return value.HasValue
            ? Math.Round((decimal)value.Value, 1, MidpointRounding.AwayFromZero)
            : 0m;
    }

    private static decimal ToMeters(double value)
    {
        return Math.Round((decimal)value, 1, MidpointRounding.AwayFromZero);
    }

    private static string ResolveAction(string? requestedAction, bool hasActiveCheckIn)
    {
        if (string.IsNullOrWhiteSpace(requestedAction))
        {
            return hasActiveCheckIn ? CheckOutAction : CheckInAction;
        }

        string normalized = requestedAction.Trim();
        if (string.Equals(normalized, CheckInAction, StringComparison.OrdinalIgnoreCase))
        {
            return CheckInAction;
        }

        if (string.Equals(normalized, CheckOutAction, StringComparison.OrdinalIgnoreCase))
        {
            return CheckOutAction;
        }

        throw new AttendanceScanException(
            AttendanceErrorCodes.InvalidAttendanceAction,
            "نوع عملیات تردد باید CheckIn یا CheckOut باشد.",
            400);
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private readonly record struct GpsReading(double Latitude, double Longitude, DateTime LocationTimestampUtc);

    private readonly record struct ShiftEvaluation(bool IsLate, bool IsEarlyDeparture, string? ShiftName);
}

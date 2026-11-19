using AriaHR.Modules.Attendance.Application;
using AriaHR.Modules.Attendance.Application.DTOs;
using AriaHR.Modules.Attendance.Application.Exceptions;
using AriaHR.Modules.Attendance.Application.Geofencing;
using AriaHR.Modules.Attendance.Application.Options;
using AriaHR.Modules.Attendance.Application.UseCases;
using AriaHR.Modules.Attendance.Infrastructure.Persistence;
using AriaHR.Modules.Attendance.Infrastructure.Repositories;
using AriaHR.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace AriaHR.Modules.Attendance.Tests;

public class ScanAttendanceTests
{
    private const decimal WorkLocationLatitude = 35.689200m;
    private const decimal WorkLocationLongitude = 51.389000m;
    private const decimal AllowedRadiusMeters = 5m;

    private static AttendanceDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AttendanceDbContext(options);
    }

    private class FakeCurrentUserService : ICurrentUserService
    {
        public Guid UserId { get; set; } = Guid.NewGuid();
        public Guid? OrganizationId { get; set; } = Guid.NewGuid();
        public IReadOnlyList<string> Roles { get; set; } = new List<string> { "Employee" };
        public bool IsInRole(string role) => Roles.Contains(role);
        public Guid ResolveOrganizationId(Guid? requestOrgId) => OrganizationId ?? Guid.Empty;
    }

    private class FakeEmployeeLookupService : IEmployeeLookupService
    {
        public Guid EmployeeId { get; set; } = Guid.NewGuid();
        public Guid OrganizationId { get; set; } = Guid.NewGuid();
        public bool HasEmployee { get; set; } = true;

        public Task<(Guid EmployeeId, Guid OrganizationId)?> GetEmployeeDetailsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            if (!HasEmployee)
            {
                return Task.FromResult<(Guid EmployeeId, Guid OrganizationId)?>(null);
            }

            return Task.FromResult<(Guid EmployeeId, Guid OrganizationId)?>((EmployeeId, OrganizationId));
        }

        public Task<IReadOnlyDictionary<Guid, (string FirstName, string LastName, string FullName)>> GetEmployeeNamesByIdsAsync(IEnumerable<Guid> employeeIds, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyDictionary<Guid, (string FirstName, string LastName, string FullName)>>(
                new Dictionary<Guid, (string FirstName, string LastName, string FullName)>());
        }
    }

    private class FakeWorkLocationLookupService : IWorkLocationLookupService
    {
        public Guid OrganizationId { get; set; } = Guid.NewGuid();
        public Guid WorkLocationId { get; set; } = Guid.NewGuid();
        public string? QrErrorCode { get; set; }
        public string? QrErrorMessage { get; set; }
        public bool LocationActive { get; set; } = true;
        public decimal AllowedRadius { get; set; } = AllowedRadiusMeters;

        public Task<WorkLocationDetailsDto?> GetWorkLocationByIdAsync(Guid workLocationId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<WorkLocationDetailsDto?>(new WorkLocationDetailsDto(
                WorkLocationId,
                OrganizationId,
                "Main Site",
                WorkLocationLatitude,
                WorkLocationLongitude,
                AllowedRadius,
                LocationActive,
                true,
                5));
        }

        public Task<WorkLocationDetailsDto?> GetWorkLocationForOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
        {
            return GetWorkLocationByIdAsync(WorkLocationId, cancellationToken);
        }

        public Task<QrCodeValidationResultDto> ValidateQrCodeAsync(string qrCodeToken, Guid? expectedOrganizationId = null, DateTime? nowUtc = null, CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(QrErrorCode))
            {
                return Task.FromResult(new QrCodeValidationResultDto(
                    false,
                    QrErrorCode,
                    null,
                    WorkLocationId,
                    OrganizationId,
                    "Dynamic",
                    QrErrorMessage ?? "کد کیوآر معتبر نمی‌باشد."));
            }

            return Task.FromResult(new QrCodeValidationResultDto(
                true,
                null,
                null,
                WorkLocationId,
                OrganizationId,
                "Dynamic",
                null));
        }
    }

    private class FakeShiftLookupService : IShiftLookupService
    {
        public EmployeeShiftAssignmentDto? Assignment { get; set; }

        public Task<EmployeeShiftAssignmentDto?> GetShiftAssignmentForEmployeeOnDateAsync(Guid employeeId, DateOnly localDate, CancellationToken cancellationToken = default)
            => Task.FromResult(Assignment);

        public Task<bool> HasAssignmentOnDateAsync(Guid employeeId, DateOnly localDate, CancellationToken cancellationToken = default)
            => Task.FromResult(Assignment != null);
    }

    private sealed class Harness : IDisposable
    {
        public required AttendanceDbContext Db { get; init; }
        public required AttendanceRecordRepository Repository { get; init; }
        public required FakeCurrentUserService CurrentUser { get; init; }
        public required FakeEmployeeLookupService EmployeeLookup { get; init; }
        public required FakeWorkLocationLookupService LocationLookup { get; init; }
        public required FakeShiftLookupService ShiftLookup { get; init; }
        public required ScanAttendanceUseCase UseCase { get; init; }

        public void Dispose() => Db.Dispose();
    }

    private static Harness CreateHarness(
        double maxGpsAccuracyMeters = 50,
        double maxAssumedUncertaintyMeters = 20,
        decimal allowedRadiusMeters = AllowedRadiusMeters)
    {
        var db = GetInMemoryDbContext();
        var repository = new AttendanceRecordRepository(db);
        var currentUser = new FakeCurrentUserService();
        var employeeLookup = new FakeEmployeeLookupService { OrganizationId = currentUser.OrganizationId!.Value };
        var locationLookup = new FakeWorkLocationLookupService
        {
            OrganizationId = employeeLookup.OrganizationId,
            AllowedRadius = allowedRadiusMeters
        };
        var shiftLookup = new FakeShiftLookupService();

        var useCase = new ScanAttendanceUseCase(
            repository,
            currentUser,
            employeeLookup,
            locationLookup,
            Options.Create(new GpsValidationOptions
            {
                MaxGpsAccuracyInMeters = maxGpsAccuracyMeters,
                MaxAssumedUncertaintyMeters = maxAssumedUncertaintyMeters,
                MaxLocationAgeSeconds = 120,
                MaxFutureSkewSeconds = 30
            }),
            Options.Create(new AttendancePolicyOptions()),
            shiftLookup);

        return new Harness
        {
            Db = db,
            Repository = repository,
            CurrentUser = currentUser,
            EmployeeLookup = employeeLookup,
            LocationLookup = locationLookup,
            ShiftLookup = shiftLookup,
            UseCase = useCase
        };
    }

    /// <summary>
    /// Latitude offset whose Haversine distance from the work location is approximately the requested metres.
    /// </summary>
    private static decimal LatitudeOffsetFor(double distanceMeters)
    {
        // Matches the Earth radius used by GeoDistanceCalculator so the offsets are exact.
        const double metersPerDegreeLatitude = 6_371_000d * Math.PI / 180d;
        return (decimal)(distanceMeters / metersPerDegreeLatitude);
    }

    private static ScanAttendanceRequest BuildRequest(
        double distanceMeters = 3,
        double accuracyMeters = 2,
        DateTime? timestamp = null,
        string? idempotencyKey = null,
        string? action = null)
    {
        return new ScanAttendanceRequest
        {
            QRCodeToken = "signed-token",
            Latitude = WorkLocationLatitude + LatitudeOffsetFor(distanceMeters),
            Longitude = WorkLocationLongitude,
            GpsAccuracyInMeters = accuracyMeters,
            LocationTimestampUtc = timestamp ?? DateTime.UtcNow,
            AttendanceAction = action,
            IdempotencyKey = idempotencyKey,
            DeviceInfo = "unit-test"
        };
    }

    [Fact]
    public async Task Scan_EmployeeWithinRadius_RegistersCheckIn()
    {
        using var harness = CreateHarness();

        var response = await harness.UseCase.ExecuteAsync(BuildRequest());

        Assert.True(response.Success);
        Assert.Equal(AttendanceErrorCodes.AttendanceRegistered, response.Code);
        Assert.Equal("CheckIn", response.Data!.AttendanceAction);
        Assert.Equal("Present", response.Data.AttendanceStatus);
        Assert.True(response.Data.DistanceInMeters < AllowedRadiusMeters);

        var record = await harness.Db.AttendanceRecords.SingleAsync();
        Assert.Equal(harness.EmployeeLookup.EmployeeId, record.EmployeeId);
        Assert.Equal(harness.LocationLookup.WorkLocationId, record.WorkLocationId);
        Assert.True(record.IsWithinGeofence);
    }

    [Fact]
    public async Task Scan_OutsideAllowedRadius_IsRejectedWithoutCreatingRecord()
    {
        using var harness = CreateHarness();

        var ex = await Assert.ThrowsAsync<AttendanceScanException>(() =>
            harness.UseCase.ExecuteAsync(BuildRequest(distanceMeters: 15)));

        Assert.Equal(AttendanceErrorCodes.OutsideAllowedRadius, ex.Code);
        Assert.NotNull(ex.Data);
        Assert.True(ex.Data!.DistanceInMeters > AllowedRadiusMeters);
        Assert.Equal(AllowedRadiusMeters, ex.Data.AllowedRadiusInMeters);
        Assert.Empty(await harness.Db.AttendanceRecords.ToListAsync());
    }

    [Fact]
    public async Task Scan_OnBoundary_IsAccepted()
    {
        using var harness = CreateHarness();

        // 4.9 m inside a 5 m radius.
        var response = await harness.UseCase.ExecuteAsync(BuildRequest(distanceMeters: 4.9));

        Assert.True(response.Success);
        Assert.Equal("CheckIn", response.Data!.AttendanceAction);
    }

    [Fact]
    public async Task Scan_InsufficientAccuracy_IsRejectedAndAllowsRetry()
    {
        using var harness = CreateHarness(maxGpsAccuracyMeters: 10);

        var ex = await Assert.ThrowsAsync<AttendanceScanException>(() =>
            harness.UseCase.ExecuteAsync(BuildRequest(distanceMeters: 3, accuracyMeters: 25)));

        Assert.Equal(AttendanceErrorCodes.LocationAccuracyInsufficient, ex.Code);
        Assert.Equal(25, ex.Data!.GpsAccuracyInMeters);
        Assert.Empty(await harness.Db.AttendanceRecords.ToListAsync());
    }

    [Theory]
    [InlineData(91, 51.389)]
    [InlineData(-91, 51.389)]
    [InlineData(35.6892, 181)]
    [InlineData(35.6892, -181)]
    public async Task Scan_InvalidCoordinates_AreRejected(double latitude, double longitude)
    {
        using var harness = CreateHarness();
        var request = BuildRequest();
        request.Latitude = (decimal)latitude;
        request.Longitude = (decimal)longitude;

        var ex = await Assert.ThrowsAsync<AttendanceScanException>(() => harness.UseCase.ExecuteAsync(request));
        Assert.Equal(AttendanceErrorCodes.LocationCoordinatesInvalid, ex.Code);
    }

    [Fact]
    public async Task Scan_OutdatedLocationTimestamp_IsRejected()
    {
        using var harness = CreateHarness();

        var ex = await Assert.ThrowsAsync<AttendanceScanException>(() =>
            harness.UseCase.ExecuteAsync(BuildRequest(timestamp: DateTime.UtcNow.AddMinutes(-10))));

        Assert.Equal(AttendanceErrorCodes.LocationDataExpired, ex.Code);
    }

    [Fact]
    public async Task Scan_FutureLocationTimestamp_IsRejected()
    {
        using var harness = CreateHarness();

        var ex = await Assert.ThrowsAsync<AttendanceScanException>(() =>
            harness.UseCase.ExecuteAsync(BuildRequest(timestamp: DateTime.UtcNow.AddMinutes(10))));

        Assert.Equal(AttendanceErrorCodes.LocationTimestampInvalid, ex.Code);
        Assert.Empty(await harness.Db.AttendanceRecords.ToListAsync());
    }

    [Fact]
    public async Task Scan_MissingGpsData_IsRejected()
    {
        using var harness = CreateHarness();

        var request = BuildRequest();
        request.Latitude = null;
        request.Longitude = null;

        var ex = await Assert.ThrowsAsync<AttendanceScanException>(() => harness.UseCase.ExecuteAsync(request));
        Assert.Equal(AttendanceErrorCodes.LocationUnavailable, ex.Code);
    }

    [Fact]
    public async Task Scan_MissingTimestamp_IsRejected()
    {
        using var harness = CreateHarness();

        var request = BuildRequest();
        request.LocationTimestampUtc = null;

        var ex = await Assert.ThrowsAsync<AttendanceScanException>(() => harness.UseCase.ExecuteAsync(request));
        Assert.Equal(AttendanceErrorCodes.LocationTimestampInvalid, ex.Code);
    }

    [Fact]
    public async Task Scan_DuplicateCheckIn_IsRejected()
    {
        using var harness = CreateHarness();

        await harness.UseCase.ExecuteAsync(BuildRequest(action: "CheckIn"));

        var ex = await Assert.ThrowsAsync<AttendanceScanException>(() =>
            harness.UseCase.ExecuteAsync(BuildRequest(accuracyMeters: 2, action: "CheckIn")));

        Assert.Equal(AttendanceErrorCodes.DuplicateAttendance, ex.Code);
        Assert.Single(await harness.Db.AttendanceRecords.ToListAsync());
    }

    [Fact]
    public async Task Scan_CheckOutWithoutActiveCheckIn_IsRejected()
    {
        using var harness = CreateHarness();

        var ex = await Assert.ThrowsAsync<AttendanceScanException>(() =>
            harness.UseCase.ExecuteAsync(BuildRequest(action: "CheckOut")));

        Assert.Equal(AttendanceErrorCodes.InvalidAttendanceAction, ex.Code);
        Assert.Empty(await harness.Db.AttendanceRecords.ToListAsync());
    }

    [Fact]
    public async Task Scan_ValidCheckOut_CompletesRecord()
    {
        using var harness = CreateHarness();

        await harness.UseCase.ExecuteAsync(BuildRequest(action: "CheckIn"));
        var response = await harness.UseCase.ExecuteAsync(BuildRequest(action: "CheckOut"));

        Assert.True(response.Success);
        Assert.Equal("CheckOut", response.Data!.AttendanceAction);
        Assert.Equal("Completed", response.Data.AttendanceStatus);

        var record = await harness.Db.AttendanceRecords.SingleAsync();
        Assert.NotNull(record.CheckOutTime);
        Assert.True(record.CheckOutDistanceInMeters.HasValue);
    }

    [Fact]
    public async Task Scan_InvalidAttendanceAction_IsRejected()
    {
        using var harness = CreateHarness();

        var ex = await Assert.ThrowsAsync<AttendanceScanException>(() =>
            harness.UseCase.ExecuteAsync(BuildRequest(action: "Something")));

        Assert.Equal(AttendanceErrorCodes.InvalidAttendanceAction, ex.Code);
    }

    [Fact]
    public async Task Scan_EmployeeFromAnotherOrganization_IsRejected()
    {
        using var harness = CreateHarness();
        harness.LocationLookup.OrganizationId = Guid.NewGuid();

        var ex = await Assert.ThrowsAsync<AttendanceScanException>(() => harness.UseCase.ExecuteAsync(BuildRequest()));
        Assert.Equal(AttendanceErrorCodes.EmployeeNotAuthorized, ex.Code);
    }

    [Fact]
    public async Task Scan_ExpiredQrCode_IsRejected()
    {
        using var harness = CreateHarness();
        harness.LocationLookup.QrErrorCode = AttendanceErrorCodes.QrCodeExpired;
        harness.LocationLookup.QrErrorMessage = "کد کیوآر منقضی شده است.";

        var ex = await Assert.ThrowsAsync<AttendanceScanException>(() => harness.UseCase.ExecuteAsync(BuildRequest()));
        Assert.Equal(AttendanceErrorCodes.QrCodeExpired, ex.Code);
    }

    [Fact]
    public async Task Scan_RepeatedIdempotencyKey_ReturnsOriginalResultWithoutDuplicating()
    {
        using var harness = CreateHarness();

        var first = await harness.UseCase.ExecuteAsync(BuildRequest(idempotencyKey: "idem-1"));
        var repeated = await harness.UseCase.ExecuteAsync(BuildRequest(idempotencyKey: "idem-1"));

        Assert.True(repeated.Success);
        Assert.True(repeated.Data!.IsReplay);
        Assert.Equal(first.Data!.AttendanceId, repeated.Data.AttendanceId);
        Assert.Single(await harness.Db.AttendanceRecords.ToListAsync());
    }

    [Fact]
    public async Task Scan_InactiveShift_IsRejected()
    {
        using var harness = CreateHarness();
        harness.ShiftLookup.Assignment = new EmployeeShiftAssignmentDto(
            Guid.NewGuid(),
            "Morning",
            new TimeOnly(8, 0),
            new TimeOnly(16, 0),
            IsShiftActive: false,
            "Assigned");

        var ex = await Assert.ThrowsAsync<AttendanceScanException>(() => harness.UseCase.ExecuteAsync(BuildRequest()));
        Assert.Equal(AttendanceErrorCodes.ShiftValidationFailed, ex.Code);
    }

    [Fact]
    public async Task Scan_TooEarlyForAssignedShift_IsRejected()
    {
        using var harness = CreateHarness();

        // Shift starts when the local clock reading is far outside the early-check-in window.
        var nowLocal = AriaHR.Shared.Utils.TimezoneHelper.GetIranLocalComponents(DateTime.UtcNow).LocalTime;
        var farFuture = nowLocal.AddHours(6);

        harness.ShiftLookup.Assignment = new EmployeeShiftAssignmentDto(
            Guid.NewGuid(),
            "Evening",
            farFuture,
            farFuture.AddHours(4),
            IsShiftActive: true,
            "Assigned");

        var ex = await Assert.ThrowsAsync<AttendanceScanException>(() => harness.UseCase.ExecuteAsync(BuildRequest()));
        Assert.Equal(AttendanceErrorCodes.ShiftValidationFailed, ex.Code);
        Assert.Empty(await harness.Db.AttendanceRecords.ToListAsync());
    }

    [Fact]
    public async Task Scan_NoShiftAssignment_DoesNotBlockExistingBehaviour()
    {
        using var harness = CreateHarness();
        harness.ShiftLookup.Assignment = null;

        var response = await harness.UseCase.ExecuteAsync(BuildRequest());
        Assert.True(response.Success);
        Assert.Null(response.Data!.ShiftName);
    }

    [Fact]
    public async Task Scan_EmployeeNotFound_IsRejected()
    {
        using var harness = CreateHarness();
        harness.EmployeeLookup.HasEmployee = false;

        var ex = await Assert.ThrowsAsync<AttendanceScanException>(() => harness.UseCase.ExecuteAsync(BuildRequest()));
        Assert.Equal(AttendanceErrorCodes.EmployeeNotFound, ex.Code);
        Assert.Equal(403, ex.StatusCode);
    }

    [Fact]
    public async Task Scan_InactiveWorkLocation_IsRejected()
    {
        using var harness = CreateHarness();
        harness.LocationLookup.LocationActive = false;

        var ex = await Assert.ThrowsAsync<AttendanceScanException>(() => harness.UseCase.ExecuteAsync(BuildRequest()));
        Assert.Equal(AttendanceErrorCodes.WorkLocationInactive, ex.Code);
        Assert.Empty(await harness.Db.AttendanceRecords.ToListAsync());
    }

    // Gate R1 — indoor case: 3 m from a 10 m location with a 30 m indoor fix. The accuracy is a
    // measurement uncertainty, not a radius, so admission must not depend on the 10 m boundary.
    [Fact]
    public async Task Scan_IndoorPoorAccuracy_WithinRadius_IsAccepted()
    {
        using var harness = CreateHarness(allowedRadiusMeters: 10m);

        var response = await harness.UseCase.ExecuteAsync(BuildRequest(distanceMeters: 3, accuracyMeters: 30));

        Assert.True(response.Success);
        Assert.Equal(AttendanceErrorCodes.AttendanceRegistered, response.Code);
        Assert.Equal("Inside", response.Data!.GeofenceOutcome);
        Assert.Equal(30, response.Data.GpsAccuracyInMeters);
        Assert.Equal(10m, response.Data.AllowedRadiusInMeters);

        var record = await harness.Db.AttendanceRecords.SingleAsync();
        Assert.Equal(30, record.CheckInAccuracy);
    }

    [Fact]
    public async Task Scan_OutdoorGoodAccuracy_WithinRadius_IsInside()
    {
        using var harness = CreateHarness(allowedRadiusMeters: 10m);

        var response = await harness.UseCase.ExecuteAsync(BuildRequest(distanceMeters: 4, accuracyMeters: 5));

        Assert.True(response.Success);
        Assert.Equal("Inside", response.Data!.GeofenceOutcome);
        Assert.Equal(5d, response.Data.UncertaintyCreditMeters);
    }

    [Fact]
    public async Task Scan_TrulyFarReading_IsOutsideAllowedRadius()
    {
        using var harness = CreateHarness(allowedRadiusMeters: 10m);

        var ex = await Assert.ThrowsAsync<AttendanceScanException>(() =>
            harness.UseCase.ExecuteAsync(BuildRequest(distanceMeters: 60, accuracyMeters: 5)));

        Assert.Equal(AttendanceErrorCodes.OutsideAllowedRadius, ex.Code);
        Assert.Equal("Outside", ex.Data!.GeofenceOutcome);
        Assert.Equal(55m, ex.Data.EffectiveDistanceMeters);
        Assert.Empty(await harness.Db.AttendanceRecords.ToListAsync());
    }

    [Fact]
    public async Task Scan_UncertainInside_IsAcceptedAndReported()
    {
        using var harness = CreateHarness(allowedRadiusMeters: 10m);

        // 25 m away with a 20 m uncertainty credit: effective distance 5 m, plausibly inside.
        var response = await harness.UseCase.ExecuteAsync(BuildRequest(distanceMeters: 25, accuracyMeters: 20));

        Assert.True(response.Success);
        Assert.Equal("UncertainInside", response.Data!.GeofenceOutcome);
        Assert.Equal(20, response.Data.UncertaintyCreditMeters);
        Assert.True(response.Data.EffectiveDistanceMeters <= 10m);
    }

    [Fact]
    public async Task Scan_VeryPoorFix_IsRejectedByAbsoluteAccuracyGate()
    {
        using var harness = CreateHarness(allowedRadiusMeters: 10m);

        var ex = await Assert.ThrowsAsync<AttendanceScanException>(() =>
            harness.UseCase.ExecuteAsync(BuildRequest(distanceMeters: 3, accuracyMeters: 120)));

        Assert.Equal(AttendanceErrorCodes.LocationAccuracyInsufficient, ex.Code);
        Assert.Equal(120, ex.Data!.GpsAccuracyInMeters);
        Assert.Empty(await harness.Db.AttendanceRecords.ToListAsync());
    }

    [Fact]
    public async Task Scan_SpoofedHugeAccuracy_IsRejectedAtAdmission()
    {
        using var harness = CreateHarness(allowedRadiusMeters: 10m);

        var ex = await Assert.ThrowsAsync<AttendanceScanException>(() =>
            harness.UseCase.ExecuteAsync(BuildRequest(distanceMeters: 60, accuracyMeters: 100_000)));

        Assert.Equal(AttendanceErrorCodes.LocationAccuracyInsufficient, ex.Code);
        Assert.Empty(await harness.Db.AttendanceRecords.ToListAsync());
    }

    [Fact]
    public async Task Scan_AccuracyCredit_IsCappedCannotReachInsideFromFarAway()
    {
        using var harness = CreateHarness(allowedRadiusMeters: 10m);

        // 50 m is admitted by R1 but the credit is capped at 20 m, so 60 m stays outside.
        var ex = await Assert.ThrowsAsync<AttendanceScanException>(() =>
            harness.UseCase.ExecuteAsync(BuildRequest(distanceMeters: 60, accuracyMeters: 50)));

        Assert.Equal(AttendanceErrorCodes.OutsideAllowedRadius, ex.Code);
        Assert.Equal(20, ex.Data!.UncertaintyCreditMeters);
        Assert.Equal(40m, ex.Data.EffectiveDistanceMeters);
    }

    [Fact]
    public async Task Scan_BoundaryOutsideWithoutUncertainty_IsRejected()
    {
        using var harness = CreateHarness(allowedRadiusMeters: 10m);

        var ex = await Assert.ThrowsAsync<AttendanceScanException>(() =>
            harness.UseCase.ExecuteAsync(BuildRequest(distanceMeters: 10.1, accuracyMeters: 0)));

        Assert.Equal(AttendanceErrorCodes.OutsideAllowedRadius, ex.Code);
    }

    [Theory]
    [InlineData(10.0, 0.0, 10.0, GeofenceOutcome.Inside)]
    [InlineData(10.1, 0.0, 10.0, GeofenceOutcome.Outside)]
    [InlineData(10.1, 5.0, 10.0, GeofenceOutcome.UncertainInside)]
    [InlineData(30.0, 100_000.0, 10.0, GeofenceOutcome.UncertainInside)]
    [InlineData(60.0, 100_000.0, 10.0, GeofenceOutcome.Outside)]
    public void GeofenceEvaluator_ClassifiesBoundaryAndCappedUncertainty(
        double distanceMeters,
        double accuracyMeters,
        double allowedRadiusMeters,
        GeofenceOutcome expected)
    {
        var evaluation = GeofenceEvaluator.Evaluate(distanceMeters, accuracyMeters, allowedRadiusMeters, 20);

        Assert.Equal(expected, evaluation.Outcome);
        Assert.True(evaluation.UncertaintyCreditMeters <= 20);
        Assert.Equal(distanceMeters - evaluation.UncertaintyCreditMeters, evaluation.EffectiveDistanceInMeters, 6);
    }

    [Fact]
    public async Task Scan_MissingAccuracy_IsRejectedAsInvalidCoordinates()
    {
        using var harness = CreateHarness(allowedRadiusMeters: 10m);
        var request = BuildRequest(distanceMeters: 3);
        request.GpsAccuracyInMeters = null;

        var ex = await Assert.ThrowsAsync<AttendanceScanException>(() => harness.UseCase.ExecuteAsync(request));
        Assert.Equal(AttendanceErrorCodes.LocationCoordinatesInvalid, ex.Code);
        Assert.Empty(await harness.Db.AttendanceRecords.ToListAsync());
    }

    [Fact]
    public async Task Scan_NegativeAccuracy_IsRejectedAsInvalidCoordinates()
    {
        using var harness = CreateHarness(allowedRadiusMeters: 10m);

        var ex = await Assert.ThrowsAsync<AttendanceScanException>(() =>
            harness.UseCase.ExecuteAsync(BuildRequest(distanceMeters: 3, accuracyMeters: -1)));

        Assert.Equal(AttendanceErrorCodes.LocationCoordinatesInvalid, ex.Code);
    }

    [Fact]
    public async Task Scan_NaN_Accuracy_IsRejectedAsInvalidCoordinates()
    {
        using var harness = CreateHarness(allowedRadiusMeters: 10m);

        var ex = await Assert.ThrowsAsync<AttendanceScanException>(() =>
            harness.UseCase.ExecuteAsync(BuildRequest(distanceMeters: 3, accuracyMeters: double.NaN)));

        Assert.Equal(AttendanceErrorCodes.LocationCoordinatesInvalid, ex.Code);
    }
}

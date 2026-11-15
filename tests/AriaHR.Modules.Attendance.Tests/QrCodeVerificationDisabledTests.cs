using AriaHR.Modules.Attendance.Application.DTOs;
using AriaHR.Modules.Attendance.Application.UseCases;
using AriaHR.Modules.Attendance.Infrastructure.Persistence;
using AriaHR.Modules.Attendance.Infrastructure.Repositories;
using AriaHR.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AriaHR.Modules.Attendance.Tests;

public class QrCodeVerificationDisabledTests
{
    private sealed class StubCurrentUserService : ICurrentUserService
    {
        public Guid UserId { get; set; } = Guid.NewGuid();
        public Guid? OrganizationId { get; set; } = Guid.NewGuid();
        public IReadOnlyList<string> Roles { get; set; } = new List<string> { "Employee" };

        public bool IsInRole(string role) => Roles.Contains(role);
        public Guid ResolveOrganizationId(Guid? requestOrgId = null) => OrganizationId ?? Guid.Empty;
    }

    private sealed class StubEmployeeLookupService : IEmployeeLookupService
    {
        public Guid EmployeeId { get; set; } = Guid.NewGuid();
        public Guid OrganizationId { get; set; } = Guid.NewGuid();

        public Task<(Guid EmployeeId, Guid OrganizationId)?> GetEmployeeDetailsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<(Guid EmployeeId, Guid OrganizationId)?>((EmployeeId, OrganizationId));
        }

        public Task<IReadOnlyDictionary<Guid, (string FirstName, string LastName, string FullName)>> GetEmployeeNamesByIdsAsync(IEnumerable<Guid> employeeIds, CancellationToken cancellationToken = default)
        {
            var dict = new Dictionary<Guid, (string FirstName, string LastName, string FullName)>
            {
                { EmployeeId, ("John", "Doe", "John Doe") }
            };

            return Task.FromResult<IReadOnlyDictionary<Guid, (string FirstName, string LastName, string FullName)>>(dict);
        }
    }

    private sealed class StubWorkLocationLookupService : IWorkLocationLookupService
    {
        public bool IsQrValid { get; set; }
        public bool QrCodeVerificationEnabled { get; set; } = true;

        public Task<WorkLocationDetailsDto?> GetWorkLocationByIdAsync(Guid workLocationId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<WorkLocationDetailsDto?>(new WorkLocationDetailsDto(
                workLocationId,
                Guid.NewGuid(),
                35.6892,
                51.3890,
                200,
                true,
                QrCodeVerificationEnabled));
        }

        public Task<WorkLocationDetailsDto?> GetWorkLocationForOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<WorkLocationDetailsDto?>(new WorkLocationDetailsDto(
                Guid.NewGuid(),
                organizationId,
                35.6892,
                51.3890,
                200,
                true,
                QrCodeVerificationEnabled));
        }

        public Task<QrCodeValidationResultDto> ValidateQrCodeAsync(string qrCodeToken, Guid? expectedOrganizationId = null, CancellationToken cancellationToken = default)
        {
            if (!IsQrValid)
            {
                return Task.FromResult(new QrCodeValidationResultDto(false, null, null, null, "کد کیوآر منقضی شده یا معتبر نمی‌باشد."));
            }

            return Task.FromResult(new QrCodeValidationResultDto(true, Guid.NewGuid(), Guid.NewGuid(), expectedOrganizationId, null));
        }
    }

    private static AttendanceDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AttendanceDbContext(options);
    }

    [Fact]
    public async Task CheckIn_WithQrToken_WhenVerificationDisabled_FallsBackToGpsRules()
    {
        using var db = GetInMemoryDbContext();
        var repository = new AttendanceRecordRepository(db);
        var currentUser = new StubCurrentUserService();
        var employeeLookup = new StubEmployeeLookupService();
        currentUser.OrganizationId = employeeLookup.OrganizationId;

        var locationLookup = new StubWorkLocationLookupService
        {
            IsQrValid = false,
            QrCodeVerificationEnabled = false
        };

        var useCase = new CheckInUseCase(repository, currentUser, employeeLookup, locationLookup);

        var result = await useCase.ExecuteAsync(new CheckInRequest
        {
            Method = "QRCode",
            QRCodeToken = "expired-or-unknown-token"
        });

        Assert.Equal("GPS", result.Method);
        Assert.Null(result.QRCodeId);
        Assert.False(result.IsWithinGeofence);
        Assert.Equal("Present", result.Status);
    }

    [Fact]
    public async Task CheckIn_WithQrToken_WhenVerificationEnabled_RejectsInvalidCode()
    {
        using var db = GetInMemoryDbContext();
        var repository = new AttendanceRecordRepository(db);
        var currentUser = new StubCurrentUserService();
        var employeeLookup = new StubEmployeeLookupService();
        currentUser.OrganizationId = employeeLookup.OrganizationId;

        var locationLookup = new StubWorkLocationLookupService
        {
            IsQrValid = false,
            QrCodeVerificationEnabled = true
        };

        var useCase = new CheckInUseCase(repository, currentUser, employeeLookup, locationLookup);

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(new CheckInRequest
        {
            Method = "QRCode",
            QRCodeToken = "expired-or-unknown-token"
        }));

        Assert.Empty(await db.AttendanceRecords.ToListAsync());
    }

    [Fact]
    public async Task CheckIn_WithValidQrToken_RecordsQrCodeAndLocation()
    {
        using var db = GetInMemoryDbContext();
        var repository = new AttendanceRecordRepository(db);
        var currentUser = new StubCurrentUserService();
        var employeeLookup = new StubEmployeeLookupService();
        currentUser.OrganizationId = employeeLookup.OrganizationId;

        var locationLookup = new StubWorkLocationLookupService
        {
            IsQrValid = true,
            QrCodeVerificationEnabled = true
        };

        var useCase = new CheckInUseCase(repository, currentUser, employeeLookup, locationLookup);

        var result = await useCase.ExecuteAsync(new CheckInRequest
        {
            Method = "QRCode",
            QRCodeToken = "valid-token"
        });

        Assert.Equal("QRCode", result.Method);
        Assert.NotNull(result.QRCodeId);
        Assert.NotNull(result.WorkLocationId);
        Assert.True(result.IsWithinGeofence);
    }
}

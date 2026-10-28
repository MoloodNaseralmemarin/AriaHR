using AriaHR.Modules.Attendance.Application.DTOs;
using AriaHR.Modules.Attendance.Application.UseCases;
using AriaHR.Modules.Attendance.Infrastructure.Persistence;
using AriaHR.Modules.Attendance.Infrastructure.Repositories;
using AriaHR.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AriaHR.Modules.Attendance.Tests;

public class AttendanceUseCaseTests
{
    private AttendanceDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AttendanceDbContext(options);
    }

    private class TestCurrentUserService : ICurrentUserService
    {
        public Guid UserId { get; set; } = Guid.NewGuid();
        public Guid? OrganizationId { get; set; } = Guid.NewGuid();
        public IReadOnlyList<string> Roles { get; set; } = new List<string> { "Employee" };

        public bool IsInRole(string role) => Roles.Contains(role);

        public Guid ResolveOrganizationId(Guid? requestOrgId)
        {
            if (IsInRole("SystemAdmin"))
            {
                return requestOrgId ?? OrganizationId ?? Guid.Empty;
            }
            return OrganizationId ?? Guid.Empty;
        }
    }

    private class TestEmployeeLookupService : IEmployeeLookupService
    {
        public Guid EmployeeId { get; set; } = Guid.NewGuid();
        public Guid OrganizationId { get; set; } = Guid.NewGuid();
        public string EmployeeFullName { get; set; } = "John Doe";

        public Task<(Guid EmployeeId, Guid OrganizationId)?> GetEmployeeDetailsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<(Guid EmployeeId, Guid OrganizationId)?>((EmployeeId, OrganizationId));
        }

        public Task<IReadOnlyDictionary<Guid, (string FirstName, string LastName, string FullName)>> GetEmployeeNamesByIdsAsync(IEnumerable<Guid> employeeIds, CancellationToken cancellationToken = default)
        {
            var dict = new Dictionary<Guid, (string FirstName, string LastName, string FullName)>
            {
                { EmployeeId, ("John", "Doe", EmployeeFullName) }
            };
            return Task.FromResult<IReadOnlyDictionary<Guid, (string FirstName, string LastName, string FullName)>>(dict);
        }
    }

    private class TestWorkLocationLookupService : IWorkLocationLookupService
    {
        public bool IsQrValid { get; set; } = true;
        public Guid? QrCodeId { get; set; } = Guid.NewGuid();
        public Guid? WorkLocationId { get; set; } = Guid.NewGuid();
        public string? QrErrorMessage { get; set; }

        public Task<WorkLocationDetailsDto?> GetWorkLocationByIdAsync(Guid workLocationId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<WorkLocationDetailsDto?>(new WorkLocationDetailsDto(workLocationId, Guid.NewGuid(), 35.6892, 51.3890, 200, true));
        }

        public Task<WorkLocationDetailsDto?> GetWorkLocationForOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<WorkLocationDetailsDto?>(new WorkLocationDetailsDto(Guid.NewGuid(), organizationId, 35.6892, 51.3890, 200, true));
        }

        public Task<QrCodeValidationResultDto> ValidateQrCodeAsync(string qrCodeToken, Guid? expectedOrganizationId = null, CancellationToken cancellationToken = default)
        {
            if (!IsQrValid)
            {
                return Task.FromResult(new QrCodeValidationResultDto(false, null, null, null, QrErrorMessage ?? "کد QR نامعتبر است."));
            }
            return Task.FromResult(new QrCodeValidationResultDto(true, QrCodeId, WorkLocationId, expectedOrganizationId, null));
        }
    }

    [Fact]
    public async Task CheckIn_Successful_CreatesRecordAndReturnsDto()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var repository = new AttendanceRecordRepository(db);
        var currentUser = new TestCurrentUserService();
        var empLookup = new TestEmployeeLookupService();
        var locLookup = new TestWorkLocationLookupService();

        currentUser.OrganizationId = empLookup.OrganizationId;

        var useCase = new CheckInUseCase(repository, currentUser, empLookup, locLookup);
        var request = new CheckInRequest
        {
            Method = "GPS",
            Latitude = 35.6892,
            Longitude = 51.3890,
            Accuracy = 10.0,
            Notes = "Check-in test"
        };

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(empLookup.EmployeeId, result.EmployeeId);
        Assert.Equal(empLookup.OrganizationId, result.OrganizationId);
        Assert.Equal("Present", result.Status);
        Assert.True(result.IsWithinGeofence);
        Assert.Null(result.CheckOutTime);

        var dbRecord = await db.AttendanceRecords.FirstOrDefaultAsync(r => r.Id == result.Id);
        Assert.NotNull(dbRecord);
        Assert.Equal("Present", dbRecord.Status);
    }

    [Fact]
    public async Task CheckIn_WhenAlreadyCheckedIn_ThrowsInvalidOperationException()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var repository = new AttendanceRecordRepository(db);
        var currentUser = new TestCurrentUserService();
        var empLookup = new TestEmployeeLookupService();
        var locLookup = new TestWorkLocationLookupService();

        currentUser.OrganizationId = empLookup.OrganizationId;

        var useCase = new CheckInUseCase(repository, currentUser, empLookup, locLookup);
        var request = new CheckInRequest { Method = "GPS" };

        await useCase.ExecuteAsync(request);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(request));
        Assert.Contains("ورود فعال", ex.Message);
    }

    [Fact]
    public async Task CheckIn_InvalidQrCode_ThrowsArgumentException()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var repository = new AttendanceRecordRepository(db);
        var currentUser = new TestCurrentUserService();
        var empLookup = new TestEmployeeLookupService();
        var locLookup = new TestWorkLocationLookupService
        {
            IsQrValid = false,
            QrErrorMessage = "کد QR منقضی شده است."
        };

        var useCase = new CheckInUseCase(repository, currentUser, empLookup, locLookup);
        var request = new CheckInRequest
        {
            Method = "QRCode",
            QRCodeToken = "invalid_token"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request));
        Assert.Contains("منقضی", ex.Message);
    }

    [Fact]
    public async Task CheckOut_Successful_UpdatesRecordWithCheckOutTime()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var repository = new AttendanceRecordRepository(db);
        var currentUser = new TestCurrentUserService();
        var empLookup = new TestEmployeeLookupService();
        var locLookup = new TestWorkLocationLookupService();

        var checkInUseCase = new CheckInUseCase(repository, currentUser, empLookup, locLookup);
        var checkOutUseCase = new CheckOutUseCase(repository, currentUser, empLookup);

        await checkInUseCase.ExecuteAsync(new CheckInRequest { Method = "GPS" });

        var checkOutRequest = new CheckOutRequest
        {
            Latitude = 35.6895,
            Longitude = 51.3892,
            Notes = "Finished work day"
        };

        // Act
        var result = await checkOutUseCase.ExecuteAsync(checkOutRequest);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.CheckOutTime);
        Assert.Equal("Completed", result.Status);
        Assert.Equal(35.6895, result.CheckOutLatitude);

        var active = await repository.GetActiveCheckInByEmployeeIdAsync(empLookup.EmployeeId);
        Assert.Null(active);
    }

    [Fact]
    public async Task CheckOut_WithoutActiveCheckIn_ThrowsInvalidOperationException()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var repository = new AttendanceRecordRepository(db);
        var currentUser = new TestCurrentUserService();
        var empLookup = new TestEmployeeLookupService();

        var checkOutUseCase = new CheckOutUseCase(repository, currentUser, empLookup);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => checkOutUseCase.ExecuteAsync(new CheckOutRequest()));
        Assert.Contains("خروج یافت نشد", ex.Message);
    }

    [Fact]
    public async Task GetMyStatus_ReturnsActiveCheckInOrNull()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var repository = new AttendanceRecordRepository(db);
        var currentUser = new TestCurrentUserService();
        var empLookup = new TestEmployeeLookupService();
        var locLookup = new TestWorkLocationLookupService();

        var checkInUseCase = new CheckInUseCase(repository, currentUser, empLookup, locLookup);
        var statusUseCase = new GetMyAttendanceStatusUseCase(repository, currentUser, empLookup);

        // Initial status should be null
        var initial = await statusUseCase.ExecuteAsync();
        Assert.Null(initial);

        // Check in
        await checkInUseCase.ExecuteAsync(new CheckInRequest { Method = "GPS" });

        // Status should return active check-in
        var active = await statusUseCase.ExecuteAsync();
        Assert.NotNull(active);
        Assert.Equal(empLookup.EmployeeId, active.EmployeeId);
    }

    [Fact]
    public async Task GetOrganizationAttendance_CenterManager_MismatchedOrgId_ThrowsUnauthorized()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var repository = new AttendanceRecordRepository(db);
        var currentUser = new TestCurrentUserService
        {
            Roles = new List<string> { "CenterManager" },
            OrganizationId = Guid.NewGuid()
        };
        var empLookup = new TestEmployeeLookupService();

        var orgUseCase = new GetOrganizationAttendanceUseCase(repository, currentUser, empLookup);

        // Act & Assert
        var otherOrgId = Guid.NewGuid();
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            orgUseCase.ExecuteAsync(organizationId: otherOrgId));

        Assert.Contains("عدم دسترسی", ex.Message);
    }

    [Fact]
    public async Task UpdateAttendanceStatus_CenterManager_UpdatesStatusAndApprovalInfo()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var repository = new AttendanceRecordRepository(db);
        var currentUser = new TestCurrentUserService
        {
            Roles = new List<string> { "CenterManager" },
            OrganizationId = Guid.NewGuid()
        };
        var empLookup = new TestEmployeeLookupService { OrganizationId = currentUser.OrganizationId.Value };

        var record = new Domain.Entities.AttendanceRecord
        {
            Id = Guid.NewGuid(),
            OrganizationId = currentUser.OrganizationId.Value,
            EmployeeId = empLookup.EmployeeId,
            CheckInTime = DateTime.UtcNow,
            Method = "Manual",
            Status = "Pending"
        };
        db.AttendanceRecords.Add(record);
        await db.SaveChangesAsync();

        var useCase = new UpdateAttendanceStatusUseCase(repository, currentUser, empLookup);
        var updateReq = new UpdateAttendanceStatusRequest
        {
            Status = "Approved",
            Notes = "Verified by manager"
        };

        // Act
        var result = await useCase.ExecuteAsync(record.Id, updateReq);

        // Assert
        Assert.Equal("Approved", result.Status);
        Assert.Equal(currentUser.UserId, result.ApprovedBy);
        Assert.NotNull(result.ApprovedAt);
    }
}

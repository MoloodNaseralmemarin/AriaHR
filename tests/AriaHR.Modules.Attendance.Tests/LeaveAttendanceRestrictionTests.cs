using AriaHR.Modules.Attendance.API.Controllers;
using AriaHR.Modules.Attendance.Application.DTOs;
using AriaHR.Modules.Attendance.Application.Exceptions;
using AriaHR.Modules.Attendance.Application.UseCases;
using AriaHR.Modules.Attendance.Infrastructure.Persistence;
using AriaHR.Modules.Attendance.Infrastructure.Repositories;
using AriaHR.Shared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AriaHR.Modules.Attendance.Tests;

public class LeaveAttendanceRestrictionTests
{
    private AttendanceDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
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

        public Task<(Guid EmployeeId, Guid OrganizationId)?> GetEmployeeDetailsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<(Guid EmployeeId, Guid OrganizationId)?>((EmployeeId, OrganizationId));
        }

        public Task<IReadOnlyDictionary<Guid, (string FirstName, string LastName, string FullName)>> GetEmployeeNamesByIdsAsync(IEnumerable<Guid> employeeIds, CancellationToken cancellationToken = default)
        {
            var dict = employeeIds.ToDictionary(id => id, id => ("Test", "Employee", "Test Employee"));
            return Task.FromResult<IReadOnlyDictionary<Guid, (string FirstName, string LastName, string FullName)>>(dict);
        }
    }

    private class FakeWorkLocationLookupService : IWorkLocationLookupService
    {
        public Task<WorkLocationDetailsDto?> GetWorkLocationByIdAsync(Guid workLocationId, CancellationToken cancellationToken = default) => Task.FromResult<WorkLocationDetailsDto?>(null);
        public Task<WorkLocationDetailsDto?> GetWorkLocationForOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default) => Task.FromResult<WorkLocationDetailsDto?>(null);
        public Task<QrCodeValidationResultDto> ValidateQrCodeAsync(string qrCodeToken, Guid? expectedOrganizationId = null, CancellationToken cancellationToken = default) => Task.FromResult(new QrCodeValidationResultDto(true, Guid.NewGuid(), Guid.NewGuid(), expectedOrganizationId, null));
    }

    private class FakeLeaveLookupService : ILeaveLookupService
    {
        public bool IsOnLeave { get; set; }

        public Task<bool> IsEmployeeOnApprovedLeaveAsync(Guid employeeId, DateTime utcTimestamp, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(IsOnLeave);
        }

        public Task<HashSet<Guid>> GetEmployeeIdsOnApprovedLeaveTodayAsync(Guid organizationId, DateOnly localDate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new HashSet<Guid>());
        }

        public Task<Dictionary<Guid, ApprovedLeaveInfoDto>> GetEmployeesApprovedLeaveStatusAsync(Guid organizationId, DateTime utcTimestamp, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new Dictionary<Guid, ApprovedLeaveInfoDto>());
        }
    }

    [Fact]
    public async Task CheckIn_WhenOnApprovedLeave_ThrowsLeaveRestrictionException()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var repository = new AttendanceRecordRepository(db);
        var currentUser = new FakeCurrentUserService();
        var empLookup = new FakeEmployeeLookupService();
        var locLookup = new FakeWorkLocationLookupService();
        var leaveLookup = new FakeLeaveLookupService { IsOnLeave = true };

        var useCase = new CheckInUseCase(repository, currentUser, empLookup, locLookup, leaveLookup);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<LeaveRestrictionException>(() => useCase.ExecuteAsync(new CheckInRequest()));
        Assert.Equal("ATTENDANCE_BLOCKED_BY_APPROVED_LEAVE", ex.ErrorCode);
        Assert.Contains("مرخصی", ex.Message);
    }

    [Fact]
    public async Task CheckOut_WhenOnApprovedLeave_ThrowsLeaveRestrictionException()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var repository = new AttendanceRecordRepository(db);
        var currentUser = new FakeCurrentUserService();
        var empLookup = new FakeEmployeeLookupService();
        var locLookup = new FakeWorkLocationLookupService();
        var leaveLookup = new FakeLeaveLookupService { IsOnLeave = false };

        var checkInUseCase = new CheckInUseCase(repository, currentUser, empLookup, locLookup, leaveLookup);
        var checkOutUseCase = new CheckOutUseCase(repository, currentUser, empLookup, leaveLookup);

        // Check in when not on leave
        await checkInUseCase.ExecuteAsync(new CheckInRequest());

        // Now employee's leave becomes active
        leaveLookup.IsOnLeave = true;

        // Act & Assert CheckOut
        var ex = await Assert.ThrowsAsync<LeaveRestrictionException>(() => checkOutUseCase.ExecuteAsync(new CheckOutRequest()));
        Assert.Equal("ATTENDANCE_BLOCKED_BY_APPROVED_LEAVE", ex.ErrorCode);
    }

    [Fact]
    public async Task Controller_ReturnsProblemDetailsWithErrorCode_WhenLeaveRestrictionExceptionThrown()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var repository = new AttendanceRecordRepository(db);
        var currentUser = new FakeCurrentUserService();
        var empLookup = new FakeEmployeeLookupService();
        var locLookup = new FakeWorkLocationLookupService();
        var leaveLookup = new FakeLeaveLookupService { IsOnLeave = true };

        var checkInUseCase = new CheckInUseCase(repository, currentUser, empLookup, locLookup, leaveLookup);
        var checkOutUseCase = new CheckOutUseCase(repository, currentUser, empLookup, leaveLookup);
        var statusUseCase = new GetMyAttendanceStatusUseCase(repository, currentUser, empLookup);
        var historyUseCase = new GetMyAttendanceHistoryUseCase(repository, currentUser, empLookup);
        var orgUseCase = new GetOrganizationAttendanceUseCase(repository, currentUser, empLookup);
        var updateUseCase = new UpdateAttendanceStatusUseCase(repository, currentUser, empLookup);

        var controller = new AttendanceController(
            checkInUseCase,
            checkOutUseCase,
            statusUseCase,
            historyUseCase,
            orgUseCase,
            updateUseCase);

        // Act
        var result = await controller.CheckIn(new CheckInRequest(), CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);

        Assert.Equal(StatusCodes.Status400BadRequest, problemDetails.Status);
        Assert.True(problemDetails.Extensions.ContainsKey("errorCode"));
        Assert.Equal("ATTENDANCE_BLOCKED_BY_APPROVED_LEAVE", problemDetails.Extensions["errorCode"]);
    }
}

using AriaHR.Modules.Scheduling.Application.DTOs;
using AriaHR.Modules.Scheduling.Application.UseCases.AssignShift;
using AriaHR.Modules.Scheduling.Application.UseCases.DefineShift;
using AriaHR.Modules.Scheduling.Infrastructure.Persistence;
using AriaHR.Modules.Scheduling.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AriaHR.Modules.Scheduling.Tests;

public class ShiftAssignmentTests
{
    private SchedulingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SchedulingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new SchedulingDbContext(options);
    }

    [Fact]
    public async Task AssignShift_WithValidData_ShouldCreateAssignment()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var shiftRepo = new ShiftRepository(dbContext);
        var assignmentRepo = new ShiftAssignmentRepository(dbContext);

        var defineShiftUseCase = new DefineShiftUseCase(shiftRepo);
        var assignShiftUseCase = new AssignShiftUseCase(assignmentRepo, shiftRepo);

        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        var shift = await defineShiftUseCase.ExecuteAsync(new DefineShiftRequest("شیفت روز", new TimeOnly(8, 0), new TimeOnly(16, 0)), orgId);

        var assignRequest = new AssignShiftRequest(
            EmployeeId: employeeId,
            ShiftId: shift.Id,
            Date: new DateOnly(2026, 9, 20)
        );

        // Act
        var result = await assignShiftUseCase.ExecuteAsync(assignRequest, orgId, userId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(orgId, result.OrganizationId);
        Assert.Equal(employeeId, result.EmployeeId);
        Assert.Equal(shift.Id, result.ShiftId);
        Assert.Equal("شیفت روز", result.ShiftName);
        Assert.Equal(new DateOnly(2026, 9, 20), result.Date);
        Assert.Equal("Assigned", result.Status);
        Assert.Equal(userId, result.CreatedBy);
    }

    [Fact]
    public async Task AssignShift_DuplicateAssignmentOnSameDate_ShouldThrowException()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var shiftRepo = new ShiftRepository(dbContext);
        var assignmentRepo = new ShiftAssignmentRepository(dbContext);

        var defineShiftUseCase = new DefineShiftUseCase(shiftRepo);
        var assignShiftUseCase = new AssignShiftUseCase(assignmentRepo, shiftRepo);

        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        var shift1 = await defineShiftUseCase.ExecuteAsync(new DefineShiftRequest("شیفت ۱", new TimeOnly(8, 0), new TimeOnly(16, 0)), orgId);
        var shift2 = await defineShiftUseCase.ExecuteAsync(new DefineShiftRequest("شیفت ۲", new TimeOnly(16, 0), new TimeOnly(0, 0)), orgId);

        var date = new DateOnly(2026, 9, 20);
        await assignShiftUseCase.ExecuteAsync(new AssignShiftRequest(employeeId, shift1.Id, date), orgId, userId);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            assignShiftUseCase.ExecuteAsync(new AssignShiftRequest(employeeId, shift2.Id, date), orgId, userId));
    }

    [Fact]
    public async Task AssignShift_WithNonExistentShift_ShouldThrowException()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var shiftRepo = new ShiftRepository(dbContext);
        var assignmentRepo = new ShiftAssignmentRepository(dbContext);
        var assignShiftUseCase = new AssignShiftUseCase(assignmentRepo, shiftRepo);

        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var assignRequest = new AssignShiftRequest(
            EmployeeId: Guid.NewGuid(),
            ShiftId: Guid.NewGuid(),
            Date: new DateOnly(2026, 9, 20)
        );

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            assignShiftUseCase.ExecuteAsync(assignRequest, orgId, userId));
    }
}

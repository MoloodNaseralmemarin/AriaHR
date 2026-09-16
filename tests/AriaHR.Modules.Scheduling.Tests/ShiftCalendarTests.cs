using AriaHR.Modules.Scheduling.Application.DTOs;
using AriaHR.Modules.Scheduling.Application.UseCases.AssignShift;
using AriaHR.Modules.Scheduling.Application.UseCases.DefineShift;
using AriaHR.Modules.Scheduling.Application.UseCases.GetEmployeeShiftCalendar;
using AriaHR.Modules.Scheduling.Application.UseCases.GetShiftCalendar;
using AriaHR.Modules.Scheduling.Infrastructure.Persistence;
using AriaHR.Modules.Scheduling.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AriaHR.Modules.Scheduling.Tests;

public class ShiftCalendarTests
{
    private SchedulingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SchedulingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new SchedulingDbContext(options);
    }

    [Fact]
    public async Task GetShiftCalendar_ShouldReturnAssignmentsWithinDateRange()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var shiftRepo = new ShiftRepository(dbContext);
        var assignmentRepo = new ShiftAssignmentRepository(dbContext);

        var defineShiftUseCase = new DefineShiftUseCase(shiftRepo);
        var assignShiftUseCase = new AssignShiftUseCase(assignmentRepo, shiftRepo);
        var getCalendarUseCase = new GetShiftCalendarUseCase(assignmentRepo);

        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var emp1 = Guid.NewGuid();
        var emp2 = Guid.NewGuid();

        var shift = await defineShiftUseCase.ExecuteAsync(new DefineShiftRequest("شیفت صبح", new TimeOnly(8, 0), new TimeOnly(16, 0)), orgId);

        await assignShiftUseCase.ExecuteAsync(new AssignShiftRequest(emp1, shift.Id, new DateOnly(2026, 9, 10)), orgId, userId);
        await assignShiftUseCase.ExecuteAsync(new AssignShiftRequest(emp2, shift.Id, new DateOnly(2026, 9, 15)), orgId, userId);
        await assignShiftUseCase.ExecuteAsync(new AssignShiftRequest(emp1, shift.Id, new DateOnly(2026, 9, 25)), orgId, userId);

        // Act
        var calendar = await getCalendarUseCase.ExecuteAsync(
            orgId,
            startDate: new DateOnly(2026, 9, 1),
            endDate: new DateOnly(2026, 9, 20));

        // Assert
        Assert.Equal(2, calendar.Count);
        Assert.Contains(calendar, c => c.EmployeeId == emp1 && c.Date == new DateOnly(2026, 9, 10));
        Assert.Contains(calendar, c => c.EmployeeId == emp2 && c.Date == new DateOnly(2026, 9, 15));
    }

    [Fact]
    public async Task GetEmployeeShiftCalendar_ShouldReturnAssignmentsForSpecificEmployee()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var shiftRepo = new ShiftRepository(dbContext);
        var assignmentRepo = new ShiftAssignmentRepository(dbContext);

        var defineShiftUseCase = new DefineShiftUseCase(shiftRepo);
        var assignShiftUseCase = new AssignShiftUseCase(assignmentRepo, shiftRepo);
        var getEmployeeCalendarUseCase = new GetEmployeeShiftCalendarUseCase(assignmentRepo);

        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var targetEmp = Guid.NewGuid();
        var otherEmp = Guid.NewGuid();

        var shift = await defineShiftUseCase.ExecuteAsync(new DefineShiftRequest("شیفت صبح", new TimeOnly(8, 0), new TimeOnly(16, 0)), orgId);

        await assignShiftUseCase.ExecuteAsync(new AssignShiftRequest(targetEmp, shift.Id, new DateOnly(2026, 9, 10)), orgId, userId);
        await assignShiftUseCase.ExecuteAsync(new AssignShiftRequest(otherEmp, shift.Id, new DateOnly(2026, 9, 10)), orgId, userId);

        // Act
        var calendar = await getEmployeeCalendarUseCase.ExecuteAsync(
            targetEmp,
            startDate: new DateOnly(2026, 9, 1),
            endDate: new DateOnly(2026, 9, 30));

        // Assert
        Assert.Single(calendar);
        Assert.Equal(targetEmp, calendar[0].EmployeeId);
    }
}

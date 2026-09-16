using AriaHR.Modules.Scheduling.Application.DTOs;
using AriaHR.Modules.Scheduling.Application.UseCases.DefineShift;
using AriaHR.Modules.Scheduling.Application.UseCases.GetActiveShifts;
using AriaHR.Modules.Scheduling.Application.UseCases.GetShiftById;
using AriaHR.Modules.Scheduling.Infrastructure.Persistence;
using AriaHR.Modules.Scheduling.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AriaHR.Modules.Scheduling.Tests;

public class ShiftTests
{
    private SchedulingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SchedulingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new SchedulingDbContext(options);
    }

    [Fact]
    public async Task DefineShift_WithValidData_ShouldCreateShiftSuccessfully()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var repository = new ShiftRepository(dbContext);
        var useCase = new DefineShiftUseCase(repository);

        var orgId = Guid.NewGuid();
        var request = new DefineShiftRequest(
            Name: "شیفت روزانه",
            StartTime: new TimeOnly(8, 0),
            EndTime: new TimeOnly(16, 0),
            IsActive: true
        );

        // Act
        var result = await useCase.ExecuteAsync(request, orgId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(orgId, result.OrganizationId);
        Assert.Equal("شیفت روزانه", result.Name);
        Assert.Equal(new TimeOnly(8, 0), result.StartTime);
        Assert.Equal(new TimeOnly(16, 0), result.EndTime);
        Assert.True(result.IsActive);

        var entity = await dbContext.Shifts.FindAsync(result.Id);
        Assert.NotNull(entity);
        Assert.Equal("شیفت روزانه", entity.Name);
    }

    [Fact]
    public async Task DefineShift_WithDuplicateNameInSameOrg_ShouldThrowException()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var repository = new ShiftRepository(dbContext);
        var useCase = new DefineShiftUseCase(repository);

        var orgId = Guid.NewGuid();
        var request = new DefineShiftRequest(
            Name: "شیفت شب",
            StartTime: new TimeOnly(20, 0),
            EndTime: new TimeOnly(4, 0),
            IsActive: true
        );

        await useCase.ExecuteAsync(request, orgId);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(request, orgId));
    }

    [Fact]
    public async Task GetActiveShifts_ShouldReturnOnlyActiveNonDeletedShiftsForOrg()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var repository = new ShiftRepository(dbContext);
        var defineUseCase = new DefineShiftUseCase(repository);
        var getActiveUseCase = new GetActiveShiftsUseCase(repository);

        var org1 = Guid.NewGuid();
        var org2 = Guid.NewGuid();

        await defineUseCase.ExecuteAsync(new DefineShiftRequest("شیفت ۱", new TimeOnly(8, 0), new TimeOnly(16, 0), IsActive: true), org1);
        await defineUseCase.ExecuteAsync(new DefineShiftRequest("شیفت ۲", new TimeOnly(16, 0), new TimeOnly(0, 0), IsActive: false), org1);
        await defineUseCase.ExecuteAsync(new DefineShiftRequest("شیفت ۳", new TimeOnly(8, 0), new TimeOnly(16, 0), IsActive: true), org2);

        // Act
        var resultOrg1 = await getActiveUseCase.ExecuteAsync(org1);

        // Assert
        Assert.Single(resultOrg1);
        Assert.Equal("شیفت ۱", resultOrg1[0].Name);
    }

    [Fact]
    public async Task GetShiftById_WithValidOrgId_ShouldReturnShift()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var repository = new ShiftRepository(dbContext);
        var defineUseCase = new DefineShiftUseCase(repository);
        var getByIdUseCase = new GetShiftByIdUseCase(repository);

        var orgId = Guid.NewGuid();
        var created = await defineUseCase.ExecuteAsync(new DefineShiftRequest("شیفت عصر", new TimeOnly(14, 0), new TimeOnly(22, 0)), orgId);

        // Act
        var result = await getByIdUseCase.ExecuteAsync(created.Id, orgId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal("شیفت عصر", result.Name);
    }

    [Fact]
    public async Task GetShiftById_WithWrongOrgId_ShouldReturnNull()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var repository = new ShiftRepository(dbContext);
        var defineUseCase = new DefineShiftUseCase(repository);
        var getByIdUseCase = new GetShiftByIdUseCase(repository);

        var org1 = Guid.NewGuid();
        var org2 = Guid.NewGuid();
        var created = await defineUseCase.ExecuteAsync(new DefineShiftRequest("شیفت عصر", new TimeOnly(14, 0), new TimeOnly(22, 0)), org1);

        // Act
        var result = await getByIdUseCase.ExecuteAsync(created.Id, org2);

        // Assert
        Assert.Null(result);
    }
}

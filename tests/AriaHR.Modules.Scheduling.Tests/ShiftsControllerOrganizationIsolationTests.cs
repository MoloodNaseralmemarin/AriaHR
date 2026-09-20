using System.Security.Claims;
using AriaHR.Modules.Scheduling.API.Controllers;
using AriaHR.Modules.Scheduling.Application.DTOs;
using AriaHR.Modules.Scheduling.Application.UseCases.DefineShift;
using AriaHR.Modules.Scheduling.Application.UseCases.GetActiveShifts;
using AriaHR.Modules.Scheduling.Application.UseCases.GetShiftById;
using AriaHR.Modules.Scheduling.Infrastructure.Persistence;
using AriaHR.Modules.Scheduling.Infrastructure.Repositories;
using AriaHR.Shared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AriaHR.Modules.Scheduling.Tests;

public class ShiftsControllerOrganizationIsolationTests
{
    private SchedulingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SchedulingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new SchedulingDbContext(options);
    }

    private ShiftsController CreateController(SchedulingDbContext dbContext, ClaimsPrincipal principal)
    {
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        var currentUserService = new CurrentUserService(httpContextAccessor);
        var repository = new ShiftRepository(dbContext);
        var defineUseCase = new DefineShiftUseCase(repository);
        var getActiveUseCase = new GetActiveShiftsUseCase(repository);
        var getByIdUseCase = new GetShiftByIdUseCase(repository);

        var controller = new ShiftsController(defineUseCase, getActiveUseCase, getByIdUseCase, currentUserService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContextAccessor.HttpContext
            }
        };

        return controller;
    }

    [Fact]
    public async Task DefineShift_CenterManager_AutomaticallyUsesUserOrganizationId_IgnoringMismatchedRequestBodyOrgId()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var centerManagerOrgId = Guid.NewGuid();
        var attemptOtherOrgId = Guid.NewGuid();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "CenterManager"),
            new Claim("organization_id", centerManagerOrgId.ToString())
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var controller = CreateController(dbContext, principal);

        var request = new DefineShiftRequest(
            Name: "Morning Shift",
            StartTime: new TimeOnly(8, 0),
            EndTime: new TimeOnly(16, 0),
            OrganizationId: attemptOtherOrgId
        );

        // Act
        var result = await controller.DefineShift(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problemDetails.Status);
    }

    [Fact]
    public async Task DefineShift_CenterManager_OmittedOrganizationId_SuccessfullyCreatesShiftForAssignedOrganization()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var centerManagerOrgId = Guid.NewGuid();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "CenterManager"),
            new Claim("organization_id", centerManagerOrgId.ToString())
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var controller = CreateController(dbContext, principal);

        var request = new DefineShiftRequest(
            Name: "Morning Shift",
            StartTime: new TimeOnly(8, 0),
            EndTime: new TimeOnly(16, 0),
            OrganizationId: null
        );

        // Act
        var result = await controller.DefineShift(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);

        var shiftDto = Assert.IsType<ShiftDto>(createdResult.Value);
        Assert.Equal(centerManagerOrgId, shiftDto.OrganizationId);
        Assert.Equal("Morning Shift", shiftDto.Name);

        var shiftEntity = await dbContext.Shifts.FirstOrDefaultAsync(s => s.Id == shiftDto.Id);
        Assert.NotNull(shiftEntity);
        Assert.Equal(centerManagerOrgId, shiftEntity.OrganizationId);
    }

    [Fact]
    public async Task DefineShift_CenterManager_SuccessfullyCreatesShiftForAssignedOrganization()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var centerManagerOrgId = Guid.NewGuid();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "CenterManager"),
            new Claim("organization_id", centerManagerOrgId.ToString())
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var controller = CreateController(dbContext, principal);

        var request = new DefineShiftRequest(
            Name: "Morning Shift",
            StartTime: new TimeOnly(8, 0),
            EndTime: new TimeOnly(16, 0),
            OrganizationId: centerManagerOrgId
        );

        // Act
        var result = await controller.DefineShift(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);

        var shiftDto = Assert.IsType<ShiftDto>(createdResult.Value);
        Assert.Equal(centerManagerOrgId, shiftDto.OrganizationId);
        Assert.Equal("Morning Shift", shiftDto.Name);

        var shiftEntity = await dbContext.Shifts.FirstOrDefaultAsync(s => s.Id == shiftDto.Id);
        Assert.NotNull(shiftEntity);
        Assert.Equal(centerManagerOrgId, shiftEntity.OrganizationId);
    }

    [Fact]
    public async Task DefineShift_CenterManager_MissingOrganizationClaim_ReturnsBadRequest()
    {
        // Arrange
        using var dbContext = CreateDbContext();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "CenterManager")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var controller = CreateController(dbContext, principal);

        var request = new DefineShiftRequest(
            Name: "Morning Shift",
            StartTime: new TimeOnly(8, 0),
            EndTime: new TimeOnly(16, 0)
        );

        // Act
        var result = await controller.DefineShift(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problemDetails.Status);
        Assert.Equal("سازمان مشخص نشده است", problemDetails.Title);
    }

    [Fact]
    public async Task DefineShift_SystemAdmin_CanSpecifyTargetOrganizationId()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var targetOrgId = Guid.NewGuid();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "SystemAdmin")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var controller = CreateController(dbContext, principal);

        var request = new DefineShiftRequest(
            Name: "Admin Shift",
            StartTime: new TimeOnly(9, 0),
            EndTime: new TimeOnly(17, 0),
            OrganizationId: targetOrgId
        );

        // Act
        var result = await controller.DefineShift(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);

        var shiftDto = Assert.IsType<ShiftDto>(createdResult.Value);
        Assert.Equal(targetOrgId, shiftDto.OrganizationId);
    }

    [Fact]
    public async Task GetShiftById_CenterManager_CannotAccessAnotherOrganizationShift()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var repository = new ShiftRepository(dbContext);
        var defineUseCase = new DefineShiftUseCase(repository);

        var org1 = Guid.NewGuid();
        var org2 = Guid.NewGuid();

        var createdShift = await defineUseCase.ExecuteAsync(
            new DefineShiftRequest("Org1 Shift", new TimeOnly(8, 0), new TimeOnly(16, 0)), org1);

        var claimsCenterManagerOrg2 = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "CenterManager"),
            new Claim("organization_id", org2.ToString())
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claimsCenterManagerOrg2, "TestAuth"));

        var controller = CreateController(dbContext, principal);

        // Act
        var result = await controller.GetShiftById(createdShift.Id, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }
}

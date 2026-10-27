using AriaHR.Modules.Requests.Application.DTOs;
using AriaHR.Modules.Requests.Application.Repositories;
using AriaHR.Modules.Requests.Application.UseCases.GetOrganizationLeaveRequests;
using AriaHR.Modules.Requests.Domain.Entities;
using AriaHR.Modules.Requests.Domain.Enums;
using AriaHR.Modules.Requests.Infrastructure.Persistence;
using AriaHR.Modules.Requests.Infrastructure.Repositories;
using AriaHR.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;
using LeaveTypeEnum = AriaHR.Modules.Requests.Domain.Enums.LeaveType;

namespace AriaHR.Modules.Requests.Tests;

public class GetOrganizationLeaveRequestsTests
{
    private RequestsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<RequestsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new RequestsDbContext(options);
    }

    private class TestEmployeeLookupService : IEmployeeLookupService
    {
        private readonly Dictionary<Guid, (string FirstName, string LastName, string FullName)> _names;

        public TestEmployeeLookupService(Dictionary<Guid, (string FirstName, string LastName, string FullName)> names)
        {
            _names = names;
        }

        public Task<(Guid EmployeeId, Guid OrganizationId)?> GetEmployeeDetailsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<(Guid, Guid)?>(null);
        }

        public Task<IReadOnlyDictionary<Guid, (string FirstName, string LastName, string FullName)>> GetEmployeeNamesByIdsAsync(
            IEnumerable<Guid> employeeIds,
            CancellationToken cancellationToken = default)
        {
            var dict = new Dictionary<Guid, (string FirstName, string LastName, string FullName)>();
            foreach (var id in employeeIds)
            {
                if (_names.TryGetValue(id, out var val))
                {
                    dict[id] = val;
                }
            }
            return Task.FromResult<IReadOnlyDictionary<Guid, (string, string, string)>>(dict);
        }
    }

    [Fact]
    public async Task GetOrganizationRequests_ReturnsEnrichedRequestsForOrganization()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);

        var orgId = Guid.NewGuid();
        var employee1Id = Guid.NewGuid();
        var employee2Id = Guid.NewGuid();

        var category1 = new LeaveCategory
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Name = "مرخصی استحقاقی",
            IsActive = true
        };
        var category2 = new LeaveCategory
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Name = "مرخصی استعلاجی",
            IsActive = true
        };
        await categoryRepository.AddAsync(category1);
        await categoryRepository.AddAsync(category2);

        var dailyRequest = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee1Id,
            OrganizationId = orgId,
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = category1.Id,
            StartDate = new DateOnly(2026, 11, 1),
            EndDate = new DateOnly(2026, 11, 3),
            Reason = "Personal trip",
            Status = RequestStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10)
        };

        var hourlyRequest = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee2Id,
            OrganizationId = orgId,
            LeaveType = LeaveTypeEnum.Hourly,
            LeaveCategoryId = category2.Id,
            StartDate = new DateOnly(2026, 11, 2),
            EndDate = new DateOnly(2026, 11, 2),
            StartTime = new TimeOnly(8, 30),
            EndTime = new TimeOnly(10, 0),
            Reason = "Doctor visit",
            Status = RequestStatus.Approved,
            CreatedAtUtc = DateTime.UtcNow
        };

        await repository.AddAsync(dailyRequest);
        await repository.AddAsync(hourlyRequest);
        await repository.SaveChangesAsync();

        var lookupService = new TestEmployeeLookupService(new Dictionary<Guid, (string, string, string)>
        {
            { employee1Id, ("علی", "محمدی", "علی محمدی") },
            { employee2Id, ("رضا", "احمدی", "رضا احمدی") }
        });

        var useCase = new GetOrganizationLeaveRequestsUseCase(repository, categoryRepository, lookupService);

        var results = (await useCase.ExecuteAsync(orgId, null)).ToList();

        Assert.Equal(2, results.Count);

        var dailyDto = results.First(r => r.Id == dailyRequest.Id);
        Assert.Equal("علی", dailyDto.EmployeeFirstName);
        Assert.Equal("محمدی", dailyDto.EmployeeLastName);
        Assert.Equal("علی محمدی", dailyDto.EmployeeFullName);
        Assert.Equal("مرخصی استحقاقی", dailyDto.LeaveCategoryName);
        Assert.Equal(LeaveTypeEnum.Daily, dailyDto.LeaveType);
        Assert.Equal(new DateOnly(2026, 11, 1), dailyDto.StartDate);
        Assert.Equal(new DateOnly(2026, 11, 3), dailyDto.EndDate);
        Assert.Null(dailyDto.StartTime);
        Assert.Null(dailyDto.EndTime);

        var hourlyDto = results.First(r => r.Id == hourlyRequest.Id);
        Assert.Equal("رضا", hourlyDto.EmployeeFirstName);
        Assert.Equal("احمدی", hourlyDto.EmployeeLastName);
        Assert.Equal("رضا احمدی", hourlyDto.EmployeeFullName);
        Assert.Equal("مرخصی استعلاجی", hourlyDto.LeaveCategoryName);
        Assert.Equal(LeaveTypeEnum.Hourly, hourlyDto.LeaveType);
        Assert.Equal(new DateOnly(2026, 11, 2), hourlyDto.StartDate);
        Assert.Equal(new DateOnly(2026, 11, 2), hourlyDto.EndDate);
        Assert.Equal(new TimeOnly(8, 30), hourlyDto.StartTime);
        Assert.Equal(new TimeOnly(10, 0), hourlyDto.EndTime);
    }

    [Fact]
    public async Task GetOrganizationRequests_ExcludesOtherOrganizations()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);

        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        var requestOrgA = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            OrganizationId = orgA,
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = Guid.NewGuid(),
            StartDate = new DateOnly(2026, 11, 1),
            EndDate = new DateOnly(2026, 11, 1),
            Status = RequestStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        var requestOrgB = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            OrganizationId = orgB,
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = Guid.NewGuid(),
            StartDate = new DateOnly(2026, 11, 1),
            EndDate = new DateOnly(2026, 11, 1),
            Status = RequestStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        await repository.AddAsync(requestOrgA);
        await repository.AddAsync(requestOrgB);
        await repository.SaveChangesAsync();

        var lookupService = new TestEmployeeLookupService(new Dictionary<Guid, (string, string, string)>());
        var useCase = new GetOrganizationLeaveRequestsUseCase(repository, categoryRepository, lookupService);

        var resultsOrgA = (await useCase.ExecuteAsync(orgA, null)).ToList();

        Assert.Single(resultsOrgA);
        Assert.Equal(requestOrgA.Id, resultsOrgA[0].Id);
        Assert.Equal(orgA, resultsOrgA[0].OrganizationId);
    }
}

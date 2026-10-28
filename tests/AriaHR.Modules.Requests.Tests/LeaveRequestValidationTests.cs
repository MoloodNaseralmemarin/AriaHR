using AriaHR.Modules.Requests.Application.DTOs;
using AriaHR.Shared.Services;
using AriaHR.Modules.Requests.Application.UseCases.CreateLeaveRequest;
using AriaHR.Modules.Requests.Domain.Entities;
using AriaHR.Modules.Requests.Domain.Enums;
using AriaHR.Modules.Requests.Infrastructure.Persistence;
using AriaHR.Modules.Requests.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;
using LeaveTypeEnum = AriaHR.Modules.Requests.Domain.Enums.LeaveType;

namespace AriaHR.Modules.Requests.Tests;

public class LeaveRequestValidationTests
{
    private RequestsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<RequestsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new RequestsDbContext(options);
    }

    private class MockEmployeeLookupService : IEmployeeLookupService
    {
        private readonly Guid _employeeId;
        private readonly Guid _organizationId;

        public MockEmployeeLookupService(Guid employeeId, Guid organizationId)
        {
            _employeeId = employeeId;
            _organizationId = organizationId;
        }

        public Task<(Guid EmployeeId, Guid OrganizationId)?> GetEmployeeDetailsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            if (userId == Guid.Empty) return Task.FromResult<(Guid, Guid)?>(null);
            return Task.FromResult<(Guid, Guid)?>((_employeeId, _organizationId));
        }

        public Task<IReadOnlyDictionary<Guid, (string FirstName, string LastName, string FullName)>> GetEmployeeNamesByIdsAsync(IEnumerable<Guid> employeeIds, CancellationToken cancellationToken = default)
        {
            IReadOnlyDictionary<Guid, (string FirstName, string LastName, string FullName)> result = new Dictionary<Guid, (string, string, string)>();
            return Task.FromResult(result);
        }
    }

    private async Task<LeaveCategory> SeedCategoryAsync(RequestsDbContext dbContext, Guid orgId, bool isActive = true, bool isDeleted = false)
    {
        var category = new LeaveCategory
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Name = "مرخصی استحقاقی",
            IsActive = isActive,
            IsDeleted = isDeleted,
            CreatedAtUtc = DateTime.UtcNow
        };
        dbContext.LeaveCategories.Add(category);
        await dbContext.SaveChangesAsync();
        return category;
    }

    [Fact]
    public async Task CreateLeaveRequest_HourlyWithValidData_ReturnsSuccess()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var userId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var category = await SeedCategoryAsync(dbContext, orgId);
        var lookupService = new MockEmployeeLookupService(employeeId, orgId);

        var useCase = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService);

        var request = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Hourly,
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2026, 10, 5),
            EndDate = new DateOnly(2026, 10, 5),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(11, 0),
            Reason = "Medical appointment"
        };

        var result = await useCase.ExecuteAsync(request, userId);

        Assert.NotNull(result);
        Assert.Equal(employeeId, result.EmployeeId);
        Assert.Equal(orgId, result.OrganizationId);
        Assert.Equal(category.Id, result.LeaveCategoryId);
        Assert.Equal(LeaveTypeEnum.Hourly, result.LeaveType);
        Assert.Equal(RequestStatus.Pending, result.Status);
        Assert.Equal(new DateOnly(2026, 10, 5), result.StartDate);
        Assert.Equal(new DateOnly(2026, 10, 5), result.EndDate);
        Assert.Equal(new TimeOnly(9, 0), result.StartTime);
        Assert.Equal(new TimeOnly(11, 0), result.EndTime);
    }

    [Fact]
    public async Task CreateLeaveRequest_DailyWithTimeValues_IgnoresTimeValuesAndSucceeds()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var userId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var category = await SeedCategoryAsync(dbContext, orgId);
        var lookupService = new MockEmployeeLookupService(employeeId, orgId);

        var useCase = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService);

        var request = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2026, 10, 6),
            EndDate = new DateOnly(2026, 10, 6),
            StartTime = new TimeOnly(10, 0), // Should be ignored for daily
            EndTime = new TimeOnly(12, 0),   // Should be ignored for daily
            Reason = "Personal leave"
        };

        var result = await useCase.ExecuteAsync(request, userId);

        Assert.NotNull(result);
        Assert.Equal(LeaveTypeEnum.Daily, result.LeaveType);
        Assert.Null(result.StartTime);
        Assert.Null(result.EndTime);

        var persisted = await repository.GetByIdAsync(result.Id);
        Assert.NotNull(persisted);
        Assert.Null(persisted.StartTime);
        Assert.Null(persisted.EndTime);
    }

    [Fact]
    public async Task CreateLeaveRequest_EmptyLeaveCategoryId_ThrowsArgumentException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var userId = Guid.NewGuid();
        var lookupService = new MockEmployeeLookupService(Guid.NewGuid(), Guid.NewGuid());

        var useCase = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService);

        var request = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = Guid.Empty,
            StartDate = new DateOnly(2026, 10, 5),
            EndDate = new DateOnly(2026, 10, 5)
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, userId));
        Assert.Contains("شناسه دسته‌بندی مرخصی الزامی است", ex.Message);
    }

    [Fact]
    public async Task CreateLeaveRequest_DefaultsToPendingAndNullApprovalFields()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var userId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var category = await SeedCategoryAsync(dbContext, orgId);
        var lookupService = new MockEmployeeLookupService(employeeId, orgId);

        var useCase = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService);

        var request = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2026, 10, 6),
            EndDate = new DateOnly(2026, 10, 6)
        };

        var result = await useCase.ExecuteAsync(request, userId);

        Assert.NotNull(result);
        Assert.Equal(RequestStatus.Pending, result.Status);
        Assert.Null(result.ApprovedAt);
        Assert.Null(result.ApprovedBy);
        Assert.Null(result.RejectedReason);
    }

    [Fact]
    public async Task CreateLeaveRequest_PersistenceAndRetrieval_Succeeds()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var userId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var category = await SeedCategoryAsync(dbContext, orgId);
        var lookupService = new MockEmployeeLookupService(employeeId, orgId);

        var useCase = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService);

        var request = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2026, 10, 6),
            EndDate = new DateOnly(2026, 10, 8),
            Reason = "Annual vacation"
        };

        var created = await useCase.ExecuteAsync(request, userId);

        var fetchedEntity = await repository.GetByIdAsync(created.Id);
        Assert.NotNull(fetchedEntity);
        Assert.Equal(created.Id, fetchedEntity.Id);
        Assert.Equal(category.Id, fetchedEntity.LeaveCategoryId);
        Assert.Equal(employeeId, fetchedEntity.EmployeeId);
        Assert.Equal(orgId, fetchedEntity.OrganizationId);
        Assert.Equal(RequestStatus.Pending, fetchedEntity.Status);
        Assert.Equal(new DateOnly(2026, 10, 6), fetchedEntity.StartDate);
        Assert.Equal(new DateOnly(2026, 10, 8), fetchedEntity.EndDate);
    }

    [Fact]
    public async Task CreateLeaveRequest_DailyMultiDay_Succeeds()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var userId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var category = await SeedCategoryAsync(dbContext, orgId);
        var lookupService = new MockEmployeeLookupService(employeeId, orgId);

        var useCase = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService);

        var request = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2026, 10, 6),
            EndDate = new DateOnly(2026, 10, 10),
            Reason = "Vacation"
        };

        var result = await useCase.ExecuteAsync(request, userId);

        Assert.NotNull(result);
        Assert.Equal(LeaveTypeEnum.Daily, result.LeaveType);
        Assert.Equal(new DateOnly(2026, 10, 6), result.StartDate);
        Assert.Equal(new DateOnly(2026, 10, 10), result.EndDate);
        Assert.Null(result.StartTime);
        Assert.Null(result.EndTime);
    }

    [Fact]
    public async Task CreateLeaveRequest_NonexistentCategory_ThrowsArgumentException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var userId = Guid.NewGuid();
        var lookupService = new MockEmployeeLookupService(Guid.NewGuid(), Guid.NewGuid());

        var useCase = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService);

        var request = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = Guid.NewGuid(), // Nonexistent
            StartDate = new DateOnly(2026, 10, 5),
            EndDate = new DateOnly(2026, 10, 5)
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, userId));
        Assert.Contains("دسته‌بندی مرخصی انتخابی نامعتبر است", ex.Message);
    }

    [Fact]
    public async Task CreateLeaveRequest_InactiveCategory_ThrowsArgumentException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var category = await SeedCategoryAsync(dbContext, orgId, isActive: false);
        var lookupService = new MockEmployeeLookupService(Guid.NewGuid(), orgId);

        var useCase = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService);

        var request = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2026, 10, 5),
            EndDate = new DateOnly(2026, 10, 5)
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, userId));
        Assert.Contains("غیرفعال است", ex.Message);
    }

    [Fact]
    public async Task CreateLeaveRequest_CrossOrgCategory_ThrowsArgumentException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var userId = Guid.NewGuid();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        var categoryInOrgB = await SeedCategoryAsync(dbContext, orgB);
        var lookupService = new MockEmployeeLookupService(Guid.NewGuid(), orgA); // User belongs to Org A

        var useCase = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService);

        var request = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = categoryInOrgB.Id,
            StartDate = new DateOnly(2026, 10, 5),
            EndDate = new DateOnly(2026, 10, 5)
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, userId));
        Assert.Contains("تعلق ندارد", ex.Message);
    }

    [Fact]
    public async Task CreateLeaveRequest_EndDateBeforeStartDate_ThrowsArgumentException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var category = await SeedCategoryAsync(dbContext, orgId);
        var lookupService = new MockEmployeeLookupService(Guid.NewGuid(), orgId);

        var useCase = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService);

        var request = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2026, 10, 10),
            EndDate = new DateOnly(2026, 10, 5) // Earlier than StartDate
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, userId));
        Assert.Contains("قبل از تاریخ شروع", ex.Message);
    }

    [Fact]
    public async Task CreateLeaveRequest_HourlyDifferentStartAndEndDate_ThrowsArgumentException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var category = await SeedCategoryAsync(dbContext, orgId);
        var lookupService = new MockEmployeeLookupService(Guid.NewGuid(), orgId);

        var useCase = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService);

        var request = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Hourly,
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2026, 10, 5),
            EndDate = new DateOnly(2026, 10, 6), // Hourly across days invalid
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(11, 0)
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, userId));
        Assert.Contains("تاریخ شروع و پایان باید برابر باشد", ex.Message);
    }

    [Fact]
    public async Task CreateLeaveRequest_HourlyMissingStartTime_ThrowsArgumentException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var category = await SeedCategoryAsync(dbContext, orgId);
        var lookupService = new MockEmployeeLookupService(Guid.NewGuid(), orgId);

        var useCase = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService);

        var request = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Hourly,
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2026, 10, 5),
            EndDate = new DateOnly(2026, 10, 5),
            StartTime = null,
            EndTime = new TimeOnly(11, 0)
        };

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, userId));
    }

    [Fact]
    public async Task CreateLeaveRequest_HourlyMissingEndTime_ThrowsArgumentException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var category = await SeedCategoryAsync(dbContext, orgId);
        var lookupService = new MockEmployeeLookupService(Guid.NewGuid(), orgId);

        var useCase = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService);

        var request = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Hourly,
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2026, 10, 5),
            EndDate = new DateOnly(2026, 10, 5),
            StartTime = new TimeOnly(9, 0),
            EndTime = null
        };

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, userId));
    }

    [Fact]
    public async Task CreateLeaveRequest_HourlyEndTimeBeforeStartTime_ThrowsArgumentException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var category = await SeedCategoryAsync(dbContext, orgId);
        var lookupService = new MockEmployeeLookupService(Guid.NewGuid(), orgId);

        var useCase = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService);

        var request = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Hourly,
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2026, 10, 5),
            EndDate = new DateOnly(2026, 10, 5),
            StartTime = new TimeOnly(14, 0),
            EndTime = new TimeOnly(12, 0)
        };

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, userId));
    }

    [Fact]
    public async Task CreateLeaveRequest_ReasonExceeds500Chars_ThrowsArgumentException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var category = await SeedCategoryAsync(dbContext, orgId);
        var lookupService = new MockEmployeeLookupService(Guid.NewGuid(), orgId);

        var useCase = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService);

        var request = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2026, 10, 5),
            EndDate = new DateOnly(2026, 10, 5),
            Reason = new string('A', 501)
        };

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, userId));
    }

    [Fact]
    public async Task CreateLeaveRequest_HourlyWithNullEndDate_DefaultsEndDateToStartDate_ReturnsSuccess()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var userId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var category = await SeedCategoryAsync(dbContext, orgId);
        var lookupService = new MockEmployeeLookupService(employeeId, orgId);

        var useCase = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService);

        var request = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Hourly,
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2026, 10, 15),
            EndDate = null, // Omitted or null from Angular form
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(11, 0),
            Reason = "Dentist appointment"
        };

        var result = await useCase.ExecuteAsync(request, userId);

        Assert.NotNull(result);
        Assert.Equal(new DateOnly(2026, 10, 15), result.StartDate);
        Assert.Equal(new DateOnly(2026, 10, 15), result.EndDate);
        Assert.Equal(LeaveTypeEnum.Hourly, result.LeaveType);
    }

    [Fact]
    public async Task CreateLeaveRequest_DailyWithNullEndDate_ThrowsArgumentException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var category = await SeedCategoryAsync(dbContext, orgId);
        var lookupService = new MockEmployeeLookupService(Guid.NewGuid(), orgId);

        var useCase = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService);

        var request = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2026, 10, 15),
            EndDate = null // Invalid for daily leave
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, userId));
        Assert.Contains("تاریخ پایان مرخصی الزامی است", ex.Message);
    }

    [Fact]
    public async Task CreateLeaveRequest_DailyWithDefaultEndDate_ThrowsArgumentException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var category = await SeedCategoryAsync(dbContext, orgId);
        var lookupService = new MockEmployeeLookupService(Guid.NewGuid(), orgId);

        var useCase = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService);

        var request = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2026, 10, 15),
            EndDate = default(DateOnly) // 0001-01-01
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, userId));
        Assert.Contains("تاریخ پایان مرخصی الزامی است", ex.Message);
    }

    [Fact]
    public async Task CreateLeaveRequest_IndependentCreation_HourlyAndDailyCreatedSuccessfully()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var userId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var category = await SeedCategoryAsync(dbContext, orgId);
        var lookupService = new MockEmployeeLookupService(employeeId, orgId);

        var useCase = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService);

        // 1. Create Hourly Leave Request independently
        var hourlyRequest = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Hourly,
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2026, 10, 20),
            EndDate = null,
            StartTime = new TimeOnly(13, 0),
            EndTime = new TimeOnly(15, 0)
        };
        var hourlyResult = await useCase.ExecuteAsync(hourlyRequest, userId);
        Assert.NotNull(hourlyResult);
        Assert.Equal(LeaveTypeEnum.Hourly, hourlyResult.LeaveType);

        // 2. Create Daily Leave Request independently
        var dailyRequest = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2026, 10, 25),
            EndDate = new DateOnly(2026, 10, 27)
        };
        var dailyResult = await useCase.ExecuteAsync(dailyRequest, userId);
        Assert.NotNull(dailyResult);
        Assert.Equal(LeaveTypeEnum.Daily, dailyResult.LeaveType);

        var allRequests = await repository.GetByEmployeeIdAsync(employeeId);
        Assert.Equal(2, allRequests.Count);
    }

    [Fact]
    public async Task CreateLeaveRequest_DailySingleDaySameStartAndEndDate_ReturnsSuccess()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var userId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var category = await SeedCategoryAsync(dbContext, orgId);
        var lookupService = new MockEmployeeLookupService(employeeId, orgId);

        var useCase = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService);

        var request = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2026, 11, 1),
            EndDate = new DateOnly(2026, 11, 1)
        };

        var result = await useCase.ExecuteAsync(request, userId);

        Assert.NotNull(result);
        Assert.Equal(LeaveTypeEnum.Daily, result.LeaveType);
        Assert.Equal(new DateOnly(2026, 11, 1), result.StartDate);
        Assert.Equal(new DateOnly(2026, 11, 1), result.EndDate);
        Assert.Null(result.StartTime);
        Assert.Null(result.EndTime);
    }

    [Fact]
    public async Task CreateLeaveRequest_HourlyEqualStartAndEndTime_ThrowsArgumentException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var category = await SeedCategoryAsync(dbContext, orgId);
        var lookupService = new MockEmployeeLookupService(Guid.NewGuid(), orgId);

        var useCase = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService);

        var request = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Hourly,
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2026, 11, 2),
            EndDate = new DateOnly(2026, 11, 2),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 0)
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, userId));
        Assert.Contains("بعد از زمان شروع", ex.Message);
    }

    [Fact]
    public async Task CreateLeaveRequest_MissingStartDate_ThrowsArgumentException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var category = await SeedCategoryAsync(dbContext, orgId);
        var lookupService = new MockEmployeeLookupService(Guid.NewGuid(), orgId);

        var useCase = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService);

        var request = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = category.Id,
            StartDate = default(DateOnly), // 0001-01-01
            EndDate = new DateOnly(2026, 11, 5)
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, userId));
        Assert.Contains("تاریخ شروع مرخصی الزامی است", ex.Message);
    }

    [Fact]
    public async Task CreateLeaveRequest_InvalidLeaveType_ThrowsArgumentException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var category = await SeedCategoryAsync(dbContext, orgId);
        var lookupService = new MockEmployeeLookupService(Guid.NewGuid(), orgId);

        var useCase = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService);

        var request = new CreateLeaveRequestRequest
        {
            LeaveType = (LeaveTypeEnum)999, // Invalid enum value
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2026, 11, 5),
            EndDate = new DateOnly(2026, 11, 5)
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, userId));
        Assert.Contains("نوع مرخصی نامعتبر است", ex.Message);
    }

    [Fact]
    public async Task CreateLeaveRequest_HourlyDurationCalculation_IsCorrect()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var userId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var category = await SeedCategoryAsync(dbContext, orgId);
        var lookupService = new MockEmployeeLookupService(employeeId, orgId);

        var useCase = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService);

        var request = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Hourly,
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2026, 11, 10),
            EndDate = new DateOnly(2026, 11, 10),
            StartTime = new TimeOnly(8, 30),
            EndTime = new TimeOnly(12, 15)
        };

        var result = await useCase.ExecuteAsync(request, userId);

        Assert.NotNull(result);
        Assert.True(result.StartTime.HasValue);
        Assert.True(result.EndTime.HasValue);

        TimeSpan duration = result.EndTime.Value - result.StartTime.Value;
        Assert.Equal(TimeSpan.FromMinutes(225), duration); // 3 hours 45 mins
    }

    // --- Scenario 1: Two daily annual leave requests for the same employee and date: second rejected ---
    [Fact]
    public async Task CreateLeaveRequest_DuplicateDailySameDate_ThrowsConflictException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var userId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var category = await SeedCategoryAsync(dbContext, orgId);
        var lookupService = new MockEmployeeLookupService(employeeId, orgId);

        var useCase = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService);

        var request1 = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2026, 12, 1),
            EndDate = new DateOnly(2026, 12, 1)
        };
        await useCase.ExecuteAsync(request1, userId);

        var request2 = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2026, 12, 1),
            EndDate = new DateOnly(2026, 12, 1)
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request2, userId));
        Assert.Contains("یک درخواست مرخصی برای تاریخ یا بازه زمانی انتخاب شده از قبل وجود دارد", ex.Message);
    }

    // --- Scenario 2: Two daily leave requests with overlapping date ranges: second rejected ---
    [Fact]
    public async Task CreateLeaveRequest_OverlappingDailyRanges_ThrowsConflictException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var userId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var category = await SeedCategoryAsync(dbContext, orgId);
        var lookupService = new MockEmployeeLookupService(employeeId, orgId);

        var useCase = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService);

        var request1 = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2026, 12, 5),
            EndDate = new DateOnly(2026, 12, 10)
        };
        await useCase.ExecuteAsync(request1, userId);

        var request2 = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2026, 12, 8),
            EndDate = new DateOnly(2026, 12, 15)
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request2, userId));
        Assert.Contains("یک درخواست مرخصی برای تاریخ یا بازه زمانی انتخاب شده از قبل وجود دارد", ex.Message);
    }

    // --- Scenario 3: Daily leave and hourly leave on the same date: second rejected ---
    [Fact]
    public async Task CreateLeaveRequest_DailyAndHourlyOnSameDate_ThrowsConflictException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var userId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var category = await SeedCategoryAsync(dbContext, orgId);
        var lookupService = new MockEmployeeLookupService(employeeId, orgId);

        var useCase = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService);

        // First submit daily leave
        var daily = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2026, 12, 20),
            EndDate = new DateOnly(2026, 12, 20)
        };
        await useCase.ExecuteAsync(daily, userId);

        // Attempt hourly leave on same day
        var hourly = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Hourly,
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2026, 12, 20),
            EndDate = new DateOnly(2026, 12, 20),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(12, 0)
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(hourly, userId));
        Assert.Contains("یک درخواست مرخصی برای تاریخ یا بازه زمانی انتخاب شده از قبل وجود دارد", ex.Message);
    }

    // --- Scenario 4: Two non-overlapping hourly requests on same date: allowed ---
    [Fact]
    public async Task CreateLeaveRequest_AdjacentOrNonOverlappingHourly_Succeeds()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var userId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var category = await SeedCategoryAsync(dbContext, orgId);
        var lookupService = new MockEmployeeLookupService(employeeId, orgId);

        var useCase = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService);

        // 08:00 - 10:00
        var hourly1 = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Hourly,
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2026, 12, 22),
            EndDate = new DateOnly(2026, 12, 22),
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(10, 0)
        };
        var res1 = await useCase.ExecuteAsync(hourly1, userId);
        Assert.NotNull(res1);

        // 10:00 - 12:00 (Adjacent end-to-start, no overlap)
        var hourly2 = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Hourly,
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2026, 12, 22),
            EndDate = new DateOnly(2026, 12, 22),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(12, 0)
        };
        var res2 = await useCase.ExecuteAsync(hourly2, userId);
        Assert.NotNull(res2);
    }

    // --- Scenario 5: Two overlapping hourly requests: second rejected ---
    [Fact]
    public async Task CreateLeaveRequest_OverlappingHourly_ThrowsConflictException()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var userId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var category = await SeedCategoryAsync(dbContext, orgId);
        var lookupService = new MockEmployeeLookupService(employeeId, orgId);

        var useCase = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService);

        // 09:00 - 11:00
        var hourly1 = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Hourly,
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2026, 12, 25),
            EndDate = new DateOnly(2026, 12, 25),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(11, 0)
        };
        await useCase.ExecuteAsync(hourly1, userId);

        // 10:30 - 12:00 (Overlaps 10:30-11:00)
        var hourly2 = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Hourly,
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2026, 12, 25),
            EndDate = new DateOnly(2026, 12, 25),
            StartTime = new TimeOnly(10, 30),
            EndTime = new TimeOnly(12, 0)
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(hourly2, userId));
        Assert.Contains("یک درخواست مرخصی برای تاریخ یا بازه زمانی انتخاب شده از قبل وجود دارد", ex.Message);
    }

    // --- Scenario 6: Rejected or Cancelled requests do not block new requests ---
    [Fact]
    public async Task CreateLeaveRequest_RejectedOrCancelledRequests_DoesNotBlockNewRequest()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var userId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var category = await SeedCategoryAsync(dbContext, orgId);
        var lookupService = new MockEmployeeLookupService(employeeId, orgId);

        var useCase = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService);

        var date = new DateOnly(2026, 12, 28);

        // Seed a Rejected request
        dbContext.LeaveRequests.Add(new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            OrganizationId = orgId,
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = category.Id,
            StartDate = date,
            EndDate = date,
            Status = RequestStatus.Rejected,
            CreatedAtUtc = DateTime.UtcNow
        });

        // Seed a Cancelled request
        dbContext.LeaveRequests.Add(new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            OrganizationId = orgId,
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = category.Id,
            StartDate = date,
            EndDate = date,
            Status = RequestStatus.Cancelled,
            CreatedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        // New request for same date should succeed
        var request = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = category.Id,
            StartDate = date,
            EndDate = date
        };

        var result = await useCase.ExecuteAsync(request, userId);
        Assert.NotNull(result);
        Assert.Equal(RequestStatus.Pending, result.Status);
    }

    // --- Scenario 7: Pending and Approved requests are considered ---
    [Fact]
    public async Task CreateLeaveRequest_ApprovedRequest_CausesConflict()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var userId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var category = await SeedCategoryAsync(dbContext, orgId);
        var lookupService = new MockEmployeeLookupService(employeeId, orgId);

        var useCase = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService);

        var date = new DateOnly(2026, 12, 30);

        // Seed an Approved request
        dbContext.LeaveRequests.Add(new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            OrganizationId = orgId,
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = category.Id,
            StartDate = date,
            EndDate = date,
            Status = RequestStatus.Approved,
            CreatedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var request = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = category.Id,
            StartDate = date,
            EndDate = date
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, userId));
        Assert.Contains("یک درخواست مرخصی برای تاریخ یا بازه زمانی انتخاب شده از قبل وجود دارد", ex.Message);
    }

    // --- Scenario 8: Different employees do not block each other ---
    [Fact]
    public async Task CreateLeaveRequest_DifferentEmployees_DoNotConflict()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var orgId = Guid.NewGuid();
        var category = await SeedCategoryAsync(dbContext, orgId);

        var userId1 = Guid.NewGuid();
        var employeeId1 = Guid.NewGuid();
        var lookupService1 = new MockEmployeeLookupService(employeeId1, orgId);

        var userId2 = Guid.NewGuid();
        var employeeId2 = Guid.NewGuid();
        var lookupService2 = new MockEmployeeLookupService(employeeId2, orgId);

        var date = new DateOnly(2027, 1, 5);

        // Employee 1 submits request
        var useCase1 = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService1);
        await useCase1.ExecuteAsync(new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = category.Id,
            StartDate = date,
            EndDate = date
        }, userId1);

        // Employee 2 submits request for same date -> Should succeed
        var useCase2 = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService2);
        var res2 = await useCase2.ExecuteAsync(new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = category.Id,
            StartDate = date,
            EndDate = date
        }, userId2);

        Assert.NotNull(res2);
        Assert.Equal(employeeId2, res2.EmployeeId);
    }

    // --- Scenario 9: Exclude request when checking conflicts (Self-exclusion during update check) ---
    [Fact]
    public async Task HasConflictingLeaveRequestAsync_ExcludingSelfRequestId_DoesNotConflict()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var employeeId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var date = new DateOnly(2027, 1, 10);

        var existingRequest = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            OrganizationId = orgId,
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = Guid.NewGuid(),
            StartDate = date,
            EndDate = date,
            Status = RequestStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
        dbContext.LeaveRequests.Add(existingRequest);
        await dbContext.SaveChangesAsync();

        // Check conflict excluding existingRequest.Id -> Should be false
        bool hasConflict = await repository.HasConflictingLeaveRequestAsync(
            employeeId,
            orgId,
            LeaveTypeEnum.Daily,
            date,
            date,
            null,
            null,
            excludeRequestId: existingRequest.Id);

        Assert.False(hasConflict);
    }

    // --- Scenario 10: Concurrency protection ---
    [Fact]
    public async Task CreateLeaveRequest_ConcurrentSubmissions_OnlyOneSucceeds()
    {
        using var dbContext = CreateDbContext();
        var repository = new LeaveRequestRepository(dbContext);
        var categoryRepository = new LeaveCategoryRepository(dbContext);
        var userId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var category = await SeedCategoryAsync(dbContext, orgId);
        var lookupService = new MockEmployeeLookupService(employeeId, orgId);

        var useCase = new CreateLeaveRequestUseCase(repository, categoryRepository, lookupService);

        var request1 = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2027, 2, 1),
            EndDate = new DateOnly(2027, 2, 1)
        };

        var request2 = new CreateLeaveRequestRequest
        {
            LeaveType = LeaveTypeEnum.Daily,
            LeaveCategoryId = category.Id,
            StartDate = new DateOnly(2027, 2, 1),
            EndDate = new DateOnly(2027, 2, 1)
        };

        // Run both requests concurrently
        Task task1 = useCase.ExecuteAsync(request1, userId);
        Task task2 = useCase.ExecuteAsync(request2, userId);

        var aggregateEx = await Record.ExceptionAsync(() => Task.WhenAll(task1, task2));

        Assert.NotNull(aggregateEx);
        // At least one task should throw ArgumentException due to duplicate/overlapping leave request
        var requestsInDb = await dbContext.LeaveRequests.Where(r => r.EmployeeId == employeeId).ToListAsync();
        Assert.Single(requestsInDb);
    }
}

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
}

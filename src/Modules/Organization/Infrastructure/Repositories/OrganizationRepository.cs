using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Repositories;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Organization.Infrastructure.Repositories;

public class OrganizationRepository : IOrganizationRepository
{
    private readonly OrganizationDbContext _dbContext;

    public OrganizationRepository(OrganizationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddAsync(Domain.Entities.Organization organization, CancellationToken cancellationToken = default)
    {
        await _dbContext.Organizations.AddAsync(organization, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Organizations
            .Where(x => !x.IsDeleted && x.IsActive)
            .CountAsync(cancellationToken);
    }

    public async Task<(int TotalCount, int CreatedThisMonth)> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var nonDeletedOrgs = _dbContext.Organizations
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        var totalCount = await nonDeletedOrgs.CountAsync(cancellationToken);

        var createdThisMonth = await nonDeletedOrgs
            .Where(x => x.CreatedAtUtc >= startOfMonth)
            .CountAsync(cancellationToken);

        return (totalCount, createdThisMonth);
    }

    public async Task<IReadOnlyList<Domain.Entities.Organization>> GetRecentOrganizationsAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Organizations
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecentActivityDto>> GetRecentActivitiesAsync(int count, CancellationToken cancellationToken = default)
    {
        var activities = new List<RecentActivityDto>();

        var createdOrgs = await _dbContext.Organizations
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(count)
            .Select(x => new RecentActivityDto
            {
                Type = "OrganizationCreated",
                Title = "مرکز جدید ثبت شد",
                Description = x.Name,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        activities.AddRange(createdOrgs);

        var createdManagers = await _dbContext.Organizations
            .AsNoTracking()
            .Where(x => !x.IsDeleted && (!string.IsNullOrEmpty(x.ManagerFirstName) || !string.IsNullOrEmpty(x.ManagerLastName)))
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(count)
            .Select(x => new RecentActivityDto
            {
                Type = "CenterManagerCreated",
                Title = "مدیر مرکز ایجاد شد",
                Description = ((x.ManagerFirstName ?? "") + " " + (x.ManagerLastName ?? "")).Trim(),
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        activities.AddRange(createdManagers);

        var deactivatedOrgs = await _dbContext.Organizations
            .AsNoTracking()
            .Where(x => !x.IsDeleted && !x.IsActive && x.UpdatedAtUtc.HasValue)
            .OrderByDescending(x => x.UpdatedAtUtc!.Value)
            .Take(count)
            .Select(x => new RecentActivityDto
            {
                Type = "OrganizationDeactivated",
                Title = "مرکز غیرفعال شد",
                Description = x.Name,
                CreatedAtUtc = x.UpdatedAtUtc!.Value
            })
            .ToListAsync(cancellationToken);

        activities.AddRange(deactivatedOrgs);

        return activities
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(count)
            .ToList();
    }

    public async Task<(int TotalCount, int ActiveCount, int AwaitingCompletionCount)> GetCentersOverviewAsync(CancellationToken cancellationToken = default)
    {
        var nonDeleted = _dbContext.Organizations
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        var totalCount = await nonDeleted.CountAsync(cancellationToken);

        var activeCount = await nonDeleted
            .Where(x => x.IsActive)
            .CountAsync(cancellationToken);

        var awaitingCompletionCount = await nonDeleted
            .Where(x => string.IsNullOrEmpty(x.NationalIdentifier) ||
                        string.IsNullOrEmpty(x.Phone) ||
                        string.IsNullOrEmpty(x.Address) ||
                        !x.WorkLocations.Any(w => !w.IsDeleted && w.IsActive))
            .CountAsync(cancellationToken);

        return (totalCount, activeCount, awaitingCompletionCount);
    }

    public async Task<PagedResult<RecentCenterDto>> GetRecentCentersPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 5 : pageSize;

        var query = _dbContext.Organizations
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new RecentCenterDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                Type = x.Type,
                IsActive = x.IsActive,
                IsAwaitingCompletion = string.IsNullOrEmpty(x.NationalIdentifier) ||
                                       string.IsNullOrEmpty(x.Phone) ||
                                       string.IsNullOrEmpty(x.Address) ||
                                       !x.WorkLocations.Any(w => !w.IsDeleted && w.IsActive),
                ManagerName = (x.ManagerFirstName + " " + x.ManagerLastName).Trim(),
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<RecentCenterDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PagedResult<RecentActivityDto>> GetRecentActivitiesPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : pageSize;

        int candidateFetchCount = Math.Max(pageSize * page, 50);

        var createdOrgs = await _dbContext.Organizations
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Take(candidateFetchCount)
            .Select(x => new RecentActivityDto
            {
                Type = "OrganizationCreated",
                Title = "مرکز جدید ثبت شد",
                Description = x.Name,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var createdManagers = await _dbContext.Organizations
            .AsNoTracking()
            .Where(x => !x.IsDeleted && (!string.IsNullOrEmpty(x.ManagerFirstName) || !string.IsNullOrEmpty(x.ManagerLastName)))
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Take(candidateFetchCount)
            .Select(x => new RecentActivityDto
            {
                Type = "CenterManagerCreated",
                Title = "مدیر مرکز ایجاد شد",
                Description = ((x.ManagerFirstName ?? "") + " " + (x.ManagerLastName ?? "")).Trim(),
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var deactivatedOrgs = await _dbContext.Organizations
            .AsNoTracking()
            .Where(x => !x.IsDeleted && !x.IsActive && x.UpdatedAtUtc.HasValue)
            .OrderByDescending(x => x.UpdatedAtUtc!.Value)
            .ThenByDescending(x => x.Id)
            .Take(candidateFetchCount)
            .Select(x => new RecentActivityDto
            {
                Type = "OrganizationDeactivated",
                Title = "مرکز غیرفعال شد",
                Description = x.Name,
                CreatedAtUtc = x.UpdatedAtUtc!.Value
            })
            .ToListAsync(cancellationToken);

        var createdEmployees = await _dbContext.Employees
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Take(candidateFetchCount)
            .Select(x => new RecentActivityDto
            {
                Type = "EmployeeCreated",
                Title = "کارمند جدید ثبت شد",
                Description = $"کد پرسنلی: {x.PersonnelCode}",
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var allActivities = createdOrgs
            .Concat(createdManagers)
            .Concat(deactivatedOrgs)
            .Concat(createdEmployees)
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToList();

        int totalCount = allActivities.Count;
        var pagedItems = allActivities
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<RecentActivityDto>
        {
            Items = pagedItems,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}

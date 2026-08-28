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

    public async Task<IReadOnlyList<Application.DTOs.RecentActivityDto>> GetRecentActivitiesAsync(int count, CancellationToken cancellationToken = default)
    {
        var activities = new List<Application.DTOs.RecentActivityDto>();

        // 1. OrganizationCreated activities
        var createdOrgs = await _dbContext.Organizations
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(count)
            .Select(x => new Application.DTOs.RecentActivityDto
            {
                Type = "OrganizationCreated",
                Title = "مرکز جدید ثبت شد",
                Description = x.Name,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        activities.AddRange(createdOrgs);

        // 2. CenterManagerCreated activities
        var createdManagers = await _dbContext.Organizations
            .AsNoTracking()
            .Where(x => !x.IsDeleted && (!string.IsNullOrEmpty(x.ManagerFirstName) || !string.IsNullOrEmpty(x.ManagerLastName)))
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(count)
            .Select(x => new Application.DTOs.RecentActivityDto
            {
                Type = "CenterManagerCreated",
                Title = "مدیر مرکز ایجاد شد",
                Description = ((x.ManagerFirstName ?? "") + " " + (x.ManagerLastName ?? "")).Trim(),
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        activities.AddRange(createdManagers);

        // 3. OrganizationDeactivated activities
        var deactivatedOrgs = await _dbContext.Organizations
            .AsNoTracking()
            .Where(x => !x.IsDeleted && !x.IsActive && x.UpdatedAtUtc.HasValue)
            .OrderByDescending(x => x.UpdatedAtUtc!.Value)
            .Take(count)
            .Select(x => new Application.DTOs.RecentActivityDto
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
}

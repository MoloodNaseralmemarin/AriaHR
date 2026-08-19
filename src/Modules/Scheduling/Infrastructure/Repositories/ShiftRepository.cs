using AriaHR.Modules.Scheduling.Application.Repositories;
using AriaHR.Modules.Scheduling.Domain.Entities;
using AriaHR.Modules.Scheduling.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Scheduling.Infrastructure.Repositories;

public class ShiftRepository : IShiftRepository
{
    private readonly SchedulingDbContext _dbContext;

    public ShiftRepository(SchedulingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Shift shift, CancellationToken cancellationToken = default)
    {
        await _dbContext.Shifts.AddAsync(shift, cancellationToken);
    }

    public async Task<Shift?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Shifts
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<Shift>> GetActiveShiftsByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Shifts
            .AsNoTracking()
            .Where(s => s.OrganizationId == organizationId && s.IsActive && !s.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Shifts
            .AnyAsync(s => s.OrganizationId == organizationId && s.Name == name && !s.IsDeleted, cancellationToken);
    }

    public Task UpdateAsync(Shift shift, CancellationToken cancellationToken = default)
    {
        _dbContext.Shifts.Update(shift);
        return Task.CompletedTask;
    }
}

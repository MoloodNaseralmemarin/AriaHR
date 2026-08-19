using AriaHR.Modules.Notification.Application.Repositories;
using AriaHR.Modules.Notification.Domain.Entities;
using AriaHR.Modules.Notification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Notification.Infrastructure.Repositories;

public class UserDeviceRepository : IUserDeviceRepository
{
    private readonly NotificationDbContext _dbContext;

    public UserDeviceRepository(NotificationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddAsync(UserDevice userDevice, CancellationToken cancellationToken = default)
    {
        await _dbContext.UserDevices.AddAsync(userDevice, cancellationToken);
    }

    public async Task<UserDevice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserDevices
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<UserDevice>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserDevices
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive && !x.IsDeleted)
            .OrderByDescending(x => x.LastActiveDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserDevice?> GetByUserIdAndDeviceTokenAsync(Guid userId, string deviceToken, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserDevices
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.DeviceToken == deviceToken && !x.IsDeleted, cancellationToken);
    }

    public Task UpdateAsync(UserDevice userDevice, CancellationToken cancellationToken = default)
    {
        _dbContext.UserDevices.Update(userDevice);
        return Task.CompletedTask;
    }
}

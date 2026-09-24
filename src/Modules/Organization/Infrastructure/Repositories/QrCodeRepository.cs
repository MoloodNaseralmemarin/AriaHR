using AriaHR.Modules.Organization.Application.Repositories;
using AriaHR.Modules.Organization.Domain.Entities;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Organization.Infrastructure.Repositories;

public class QrCodeRepository : IQrCodeRepository
{
    private readonly OrganizationDbContext _dbContext;

    public QrCodeRepository(OrganizationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<WorkLocation?> GetWorkLocationByIdAsync(Guid workLocationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(w => !w.IsDeleted && w.Id == workLocationId, cancellationToken);
    }

    public async Task<List<QRCode>> GetActiveQRCodesByWorkLocationIdAsync(Guid workLocationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.QRCodes
            .Where(q => !q.IsDeleted && q.IsActive && q.WorkLocationId == workLocationId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(QRCode qrCode, CancellationToken cancellationToken = default)
    {
        await _dbContext.QRCodes.AddAsync(qrCode, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

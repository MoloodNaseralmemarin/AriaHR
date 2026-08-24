using AriaHR.Modules.Identity.Application.Repositories;
using AriaHR.Modules.Identity.Domain.Entities;
using AriaHR.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Identity.Infrastructure.Repositories;

public class OtpCodeRepository : IOtpCodeRepository
{
    private readonly IdentityDbContext _dbContext;

    public OtpCodeRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OtpCode?> GetLatestActiveCodeAsync(Guid userId, string phoneNumber, CancellationToken cancellationToken = default)
    {
        return await _dbContext.OtpCodes
            .Where(o => o.UserId == userId && o.PhoneNumber == phoneNumber && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(OtpCode otpCode, CancellationToken cancellationToken = default)
    {
        await _dbContext.OtpCodes.AddAsync(otpCode, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(OtpCode otpCode, CancellationToken cancellationToken = default)
    {
        _dbContext.OtpCodes.Update(otpCode);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task InvalidateActiveCodesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var activeCodes = await _dbContext.OtpCodes
            .Where(o => o.UserId == userId && !o.IsUsed)
            .ToListAsync(cancellationToken);

        foreach (var code in activeCodes)
        {
            code.IsUsed = true;
        }

        if (activeCodes.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

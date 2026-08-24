using AriaHR.Modules.Identity.Domain.Entities;

namespace AriaHR.Modules.Identity.Application.Repositories;

public interface IOtpCodeRepository
{
    Task<OtpCode?> GetLatestActiveCodeAsync(Guid userId, string phoneNumber, CancellationToken cancellationToken = default);
    Task AddAsync(OtpCode otpCode, CancellationToken cancellationToken = default);
    Task UpdateAsync(OtpCode otpCode, CancellationToken cancellationToken = default);
    Task InvalidateActiveCodesAsync(Guid userId, CancellationToken cancellationToken = default);
}

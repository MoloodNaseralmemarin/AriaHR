using AriaHR.Modules.Organization.Domain.Entities;

namespace AriaHR.Modules.Organization.Application.Repositories;

public interface IQrCodeRepository
{
    Task<WorkLocation?> GetWorkLocationByIdAsync(Guid workLocationId, CancellationToken cancellationToken = default);
    Task<List<QRCode>> GetActiveQRCodesByWorkLocationIdAsync(Guid workLocationId, CancellationToken cancellationToken = default);
    Task<QRCode?> GetActiveStaticQrCodeAsync(Guid workLocationId, CancellationToken cancellationToken = default);
    Task<QRCode?> GetQrCodeForWindowAsync(Guid workLocationId, DateTime windowStartUtc, CancellationToken cancellationToken = default);
    Task AddAsync(QRCode qrCode, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

using AriaHR.Modules.Organization.Infrastructure.Persistence;
using AriaHR.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Organization.Infrastructure.Services;

public class WorkLocationLookupService : IWorkLocationLookupService
{
    private readonly OrganizationDbContext _dbContext;

    public WorkLocationLookupService(OrganizationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<WorkLocationDetailsDto?> GetWorkLocationByIdAsync(
        Guid workLocationId,
        CancellationToken cancellationToken = default)
    {
        var wl = await _dbContext.WorkLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(w => !w.IsDeleted && w.Id == workLocationId, cancellationToken);

        if (wl == null)
            return null;

        return new WorkLocationDetailsDto(
            wl.Id,
            wl.OrganizationId,
            wl.Latitude,
            wl.Longitude,
            wl.RadiusInMeters,
            wl.IsActive
        );
    }

    public async Task<WorkLocationDetailsDto?> GetWorkLocationForOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var wl = await _dbContext.WorkLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(w => !w.IsDeleted && w.OrganizationId == organizationId, cancellationToken);

        if (wl == null)
            return null;

        return new WorkLocationDetailsDto(
            wl.Id,
            wl.OrganizationId,
            wl.Latitude,
            wl.Longitude,
            wl.RadiusInMeters,
            wl.IsActive
        );
    }

    public async Task<QrCodeValidationResultDto> ValidateQrCodeAsync(
        string qrCodeToken,
        Guid? expectedOrganizationId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(qrCodeToken))
        {
            return new QrCodeValidationResultDto(false, null, null, null, "کد QR معتبر نمی‌باشد.");
        }

        var qr = await _dbContext.QRCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(q => !q.IsDeleted && q.Code == qrCodeToken, cancellationToken);

        if (qr == null || !qr.IsActive)
        {
            return new QrCodeValidationResultDto(false, null, null, null, "کد QR معتبر یا فعال یافت نشد.");
        }

        var now = DateTime.UtcNow;
        if (qr.ValidFrom.HasValue && qr.ValidFrom.Value > now)
        {
            return new QrCodeValidationResultDto(false, qr.Id, qr.WorkLocationId, qr.OrganizationId, "کد QR هنوز فعال نشده است.");
        }

        if (qr.ValidTo.HasValue && qr.ValidTo.Value < now)
        {
            return new QrCodeValidationResultDto(false, qr.Id, qr.WorkLocationId, qr.OrganizationId, "کد QR منقضی شده است.");
        }

        if (expectedOrganizationId.HasValue && expectedOrganizationId.Value != Guid.Empty && qr.OrganizationId != expectedOrganizationId.Value)
        {
            return new QrCodeValidationResultDto(false, qr.Id, qr.WorkLocationId, qr.OrganizationId, "کد QR متعلق به سازمان دیگری است.");
        }

        return new QrCodeValidationResultDto(true, qr.Id, qr.WorkLocationId, qr.OrganizationId, null);
    }
}

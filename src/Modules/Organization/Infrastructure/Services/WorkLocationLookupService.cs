using AriaHR.Modules.Organization.Application.Services;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using AriaHR.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Organization.Infrastructure.Services;

public class WorkLocationLookupService : IWorkLocationLookupService
{
    private readonly OrganizationDbContext _dbContext;
    private readonly IQrCodeTokenService _qrCodeTokenService;

    public WorkLocationLookupService(
        OrganizationDbContext dbContext,
        IQrCodeTokenService qrCodeTokenService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _qrCodeTokenService = qrCodeTokenService ?? throw new ArgumentNullException(nameof(qrCodeTokenService));
    }

    public async Task<WorkLocationDetailsDto?> GetWorkLocationByIdAsync(
        Guid workLocationId,
        CancellationToken cancellationToken = default)
    {
        var wl = await _dbContext.WorkLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(w => !w.IsDeleted && w.Id == workLocationId, cancellationToken);

        return wl == null ? null : Map(wl);
    }

    public async Task<WorkLocationDetailsDto?> GetWorkLocationForOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var wl = await _dbContext.WorkLocations
            .AsNoTracking()
            .Where(w => !w.IsDeleted && w.OrganizationId == organizationId)
            .OrderBy(w => w.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return wl == null ? null : Map(wl);
    }

    public async Task<QrCodeValidationResultDto> ValidateQrCodeAsync(
        string qrCodeToken,
        Guid? expectedOrganizationId = null,
        DateTime? nowUtc = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(qrCodeToken))
        {
            return Failure("QR_CODE_INVALID", "کد کیوآر معتبر نمی‌باشد.");
        }

        DateTime now = nowUtc.HasValue ? NormalizeUtc(nowUtc.Value) : DateTime.UtcNow;

        if (_qrCodeTokenService.TryParse(qrCodeToken, out var payload) && payload != null)
        {
            return await ValidateSignedTokenAsync(payload, expectedOrganizationId, now, cancellationToken);
        }

        return await ValidateLegacyQrCodeAsync(qrCodeToken, expectedOrganizationId, now, cancellationToken);
    }

    private async Task<QrCodeValidationResultDto> ValidateSignedTokenAsync(
        QrCodeTokenPayload payload,
        Guid? expectedOrganizationId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var workLocation = await _dbContext.WorkLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(w => !w.IsDeleted && w.Id == payload.WorkLocationId, cancellationToken);

        if (workLocation == null)
        {
            return Failure("WORK_LOCATION_NOT_FOUND", "محل کار مرتبط با کد کیوآر یافت نشد.");
        }

        string modeName = workLocation.QrCodeMode.ToString();

        if (!workLocation.IsActive)
        {
            return Failure("WORK_LOCATION_INACTIVE", "محل کار مورد نظر غیرفعال است.", workLocation.Id, workLocation.OrganizationId, modeName);
        }

        if (!workLocation.QrCodeEnabled)
        {
            return Failure("QR_CODE_DISABLED", "کد کیوآر این محل کار غیرفعال است.", workLocation.Id, workLocation.OrganizationId, modeName);
        }

        if (!string.Equals(payload.Identifier, workLocation.QrCodeIdentifier, StringComparison.Ordinal))
        {
            return Failure("QR_CODE_INVALID", "کد کیوآر معتبر نمی‌باشد یا باطل شده است.", workLocation.Id, workLocation.OrganizationId, modeName);
        }

        if (payload.Mode != workLocation.QrCodeMode)
        {
            return Failure("QR_CODE_INVALID", "حالت کد کیوآر با تنظیمات محل کار هم‌خوانی ندارد.", workLocation.Id, workLocation.OrganizationId, modeName);
        }

        if (payload.Mode == Domain.Enums.QrCodeMode.Dynamic)
        {
            if (!payload.ExpiresAtUtc.HasValue || payload.ExpiresAtUtc.Value <= now)
            {
                return Failure("QR_CODE_EXPIRED", "کد کیوآر منقضی شده است. لطفاً کد جدید را اسکن کنید.", workLocation.Id, workLocation.OrganizationId, modeName);
            }
        }
        else if (payload.ExpiresAtUtc.HasValue)
        {
            return Failure("QR_CODE_INVALID", "کد کیوآر معتبر نمی‌باشد.", workLocation.Id, workLocation.OrganizationId, modeName);
        }

        if (expectedOrganizationId.HasValue && expectedOrganizationId.Value != Guid.Empty &&
            workLocation.OrganizationId != expectedOrganizationId.Value)
        {
            return Failure("EMPLOYEE_NOT_AUTHORIZED", "کد کیوآر متعلق به سازمان دیگری است.", workLocation.Id, workLocation.OrganizationId, modeName);
        }

        return new QrCodeValidationResultDto(
            true,
            null,
            null,
            workLocation.Id,
            workLocation.OrganizationId,
            modeName,
            null);
    }

    private async Task<QrCodeValidationResultDto> ValidateLegacyQrCodeAsync(
        string qrCodeToken,
        Guid? expectedOrganizationId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var qr = await _dbContext.QRCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(q => !q.IsDeleted && q.Code == qrCodeToken, cancellationToken);

        if (qr == null)
        {
            return Failure("QR_CODE_INVALID", "کد کیوآر معتبر یا فعال یافت نشد.");
        }

        if (!qr.IsActive)
        {
            return Failure("QR_CODE_DISABLED", "کد کیوآر غیرفعال شده است.", qr.WorkLocationId, qr.OrganizationId, "Legacy");
        }

        if (qr.ValidFrom.HasValue && NormalizeUtc(qr.ValidFrom.Value) > now)
        {
            return Failure("QR_CODE_INVALID", "کد کیوآر هنوز فعال نشده است.", qr.WorkLocationId, qr.OrganizationId, "Legacy");
        }

        if (qr.ValidTo.HasValue && NormalizeUtc(qr.ValidTo.Value) < now)
        {
            return Failure("QR_CODE_EXPIRED", "کد کیوآر منقضی شده است.", qr.WorkLocationId, qr.OrganizationId, "Legacy");
        }

        if (expectedOrganizationId.HasValue && expectedOrganizationId.Value != Guid.Empty &&
            qr.OrganizationId != expectedOrganizationId.Value)
        {
            return Failure("EMPLOYEE_NOT_AUTHORIZED", "کد کیوآر متعلق به سازمان دیگری است.", qr.WorkLocationId, qr.OrganizationId, "Legacy");
        }

        return new QrCodeValidationResultDto(true, null, qr.Id, qr.WorkLocationId, qr.OrganizationId, "Legacy", null);
    }

    private static WorkLocationDetailsDto Map(Domain.Entities.WorkLocation wl)
    {
        return new WorkLocationDetailsDto(
            wl.Id,
            wl.OrganizationId,
            wl.Name,
            wl.Latitude,
            wl.Longitude,
            wl.AllowedRadiusInMeters,
            wl.IsActive,
            wl.QrCodeEnabled,
            wl.QrRotationIntervalMinutes);
    }

    private static QrCodeValidationResultDto Failure(
        string errorCode,
        string message,
        Guid? workLocationId = null,
        Guid? organizationId = null,
        string? mode = null)
    {
        return new QrCodeValidationResultDto(false, errorCode, null, workLocationId, organizationId, mode, message);
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}

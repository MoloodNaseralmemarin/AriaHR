using AriaHR.Modules.Organization.Application.Services;
using AriaHR.Modules.Organization.Domain.Entities;
using AriaHR.Modules.Organization.Domain.Enums;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using AriaHR.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Organization.Infrastructure.Services;

public class WorkLocationLookupService : IWorkLocationLookupService
{
    private readonly OrganizationDbContext _dbContext;
    private readonly IQrCodeTokenService _tokenService;
    private readonly TimeProvider _timeProvider;

    public WorkLocationLookupService(
        OrganizationDbContext dbContext,
        IQrCodeTokenService? tokenService = null,
        TimeProvider? timeProvider = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _tokenService = tokenService ?? new QrCodeTokenService();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<WorkLocationDetailsDto?> GetWorkLocationByIdAsync(
        Guid workLocationId,
        CancellationToken cancellationToken = default)
    {
        WorkLocation? workLocation = await _dbContext.WorkLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(w => !w.IsDeleted && w.Id == workLocationId, cancellationToken);

        return workLocation == null ? null : MapDetails(workLocation);
    }

    public async Task<WorkLocationDetailsDto?> GetWorkLocationForOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        WorkLocation? workLocation = await _dbContext.WorkLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(w => !w.IsDeleted && w.OrganizationId == organizationId, cancellationToken);

        return workLocation == null ? null : MapDetails(workLocation);
    }

    public async Task<QrCodeValidationResultDto> ValidateQrCodeAsync(
        string qrCodeToken,
        Guid? expectedOrganizationId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(qrCodeToken))
        {
            return Invalid("کد کیوآر معتبر نمی‌باشد.");
        }

        string token = qrCodeToken.Trim();
        DateTime nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        // Dynamic tokens carry a versioned, signed format; they are resolved before the opaque
        // static token lookup so a persisted rotation row can never be treated as a static code.
        if (_tokenService.TryParseDynamicToken(token, out _, out _))
        {
            return await ValidateDynamicQrCodeAsync(token, nowUtc, expectedOrganizationId, cancellationToken);
        }

        QRCode? staticQrCode = await _dbContext.QRCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(q => !q.IsDeleted && q.Code == token, cancellationToken);

        if (staticQrCode != null)
        {
            return await ValidateStaticQrCodeAsync(staticQrCode, nowUtc, expectedOrganizationId, cancellationToken);
        }

        return Invalid("کد کیوآر معتبر یا فعال یافت نشد.");
    }

    private async Task<QrCodeValidationResultDto> ValidateStaticQrCodeAsync(
        QRCode qrCode,
        DateTime nowUtc,
        Guid? expectedOrganizationId,
        CancellationToken cancellationToken)
    {
        if (!qrCode.IsActive)
        {
            return Invalid("کد کیوآر معتبر یا فعال یافت نشد.");
        }

        WorkLocation? workLocation = await _dbContext.WorkLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(w => !w.IsDeleted && w.Id == qrCode.WorkLocationId, cancellationToken);

        if (workLocation == null || !workLocation.IsActive)
        {
            return Invalid("محل کار مرتبط با کد کیوآر فعال نمی‌باشد.");
        }

        if (!workLocation.QrCodeVerificationEnabled)
        {
            return Invalid("تأیید کد کیوآر برای این محل کار غیرفعال است.");
        }

        if (workLocation.QrCodeMode != QrCodeMode.Static)
        {
            return Invalid("این کد کیوآر برای حالت ثابت این محل کار صادر نشده است.");
        }

        if (qrCode.ValidFrom.HasValue && qrCode.ValidFrom.Value > nowUtc)
        {
            return new QrCodeValidationResultDto(false, qrCode.Id, qrCode.WorkLocationId, qrCode.OrganizationId, "کد کیوآر هنوز فعال نشده است.");
        }

        if (qrCode.ValidTo.HasValue && qrCode.ValidTo.Value <= nowUtc)
        {
            return new QrCodeValidationResultDto(false, qrCode.Id, qrCode.WorkLocationId, qrCode.OrganizationId, "کد کیوآر منقضی شده است.");
        }

        if (expectedOrganizationId.HasValue &&
            expectedOrganizationId.Value != Guid.Empty &&
            workLocation.OrganizationId != expectedOrganizationId.Value)
        {
            return new QrCodeValidationResultDto(false, qrCode.Id, qrCode.WorkLocationId, qrCode.OrganizationId, "کد کیوآر متعلق به سازمان دیگری است.");
        }

        return new QrCodeValidationResultDto(true, qrCode.Id, workLocation.Id, workLocation.OrganizationId, null);
    }

    private async Task<QrCodeValidationResultDto> ValidateDynamicQrCodeAsync(
        string token,
        DateTime nowUtc,
        Guid? expectedOrganizationId,
        CancellationToken cancellationToken)
    {
        if (!_tokenService.TryParseDynamicToken(token, out string codeReference, out _))
        {
            return Invalid("کد کیوآر معتبر نمی‌باشد.");
        }

        WorkLocation? workLocation = await _dbContext.WorkLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(w => !w.IsDeleted && w.QrCodeReference == codeReference, cancellationToken);

        if (workLocation == null)
        {
            return Invalid("کد کیوآر معتبر یا فعال یافت نشد.");
        }

        if (!workLocation.IsActive)
        {
            return Invalid("محل کار مرتبط با کد کیوآر فعال نمی‌باشد.");
        }

        if (!workLocation.QrCodeVerificationEnabled)
        {
            return Invalid("تأیید کد کیوآر برای این محل کار غیرفعال است.");
        }

        if (workLocation.QrCodeMode != QrCodeMode.Dynamic)
        {
            return Invalid("این کد کیوآر برای حالت پویا این محل کار صادر نشده است.");
        }

        if (string.IsNullOrWhiteSpace(workLocation.QrCodeSecret) ||
            !_tokenService.TryValidateDynamicToken(
                token,
                workLocation.QrCodeReference!,
                workLocation.QrCodeSecret,
                nowUtc,
                workLocation.QrCodeRotationMinutes,
                out _))
        {
            return Invalid("کد کیوآر منقضی شده یا معتبر نمی‌باشد.");
        }

        if (expectedOrganizationId.HasValue &&
            expectedOrganizationId.Value != Guid.Empty &&
            workLocation.OrganizationId != expectedOrganizationId.Value)
        {
            return Invalid("کد کیوآر متعلق به سازمان دیگری است.");
        }

        QRCode? qrCode = await _dbContext.QRCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                q => !q.IsDeleted && q.WorkLocationId == workLocation.Id && q.Code == token,
                cancellationToken);

        return new QrCodeValidationResultDto(true, qrCode?.Id, workLocation.Id, workLocation.OrganizationId, null);
    }

    private static WorkLocationDetailsDto MapDetails(WorkLocation workLocation)
    {
        return new WorkLocationDetailsDto(
            workLocation.Id,
            workLocation.OrganizationId,
            workLocation.Latitude,
            workLocation.Longitude,
            workLocation.RadiusInMeters,
            workLocation.IsActive,
            workLocation.QrCodeVerificationEnabled,
            workLocation.QrCodeMode.ToString(),
            workLocation.QrCodeRotationMinutes);
    }

    private static QrCodeValidationResultDto Invalid(string message)
    {
        return new QrCodeValidationResultDto(false, null, null, null, message);
    }
}

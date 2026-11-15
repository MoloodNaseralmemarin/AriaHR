using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Options;
using AriaHR.Modules.Organization.Application.Repositories;
using AriaHR.Modules.Organization.Application.Services;
using AriaHR.Modules.Organization.Domain.Entities;
using AriaHR.Modules.Organization.Domain.Enums;
using Microsoft.Extensions.Options;

namespace AriaHR.Modules.Organization.Application.UseCases.UpdateQrCodeSettings;

public class UpdateQrCodeSettingsUseCase : IUpdateQrCodeSettingsUseCase
{
    private readonly IQrCodeRepository _repository;
    private readonly IQrCodeService _qrCodeService;
    private readonly IQrCodeTokenService _tokenService;
    private readonly QrCodeOptions _options;
    private readonly TimeProvider _timeProvider;

    public UpdateQrCodeSettingsUseCase(
        IQrCodeRepository repository,
        IQrCodeService qrCodeService,
        IQrCodeTokenService tokenService,
        IOptions<QrCodeOptions> options,
        TimeProvider? timeProvider = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _qrCodeService = qrCodeService ?? throw new ArgumentNullException(nameof(qrCodeService));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _options = options?.Value ?? new QrCodeOptions();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<QrCodeSettingsResponse> ExecuteAsync(
        Guid workLocationId,
        UpdateQrCodeSettingsRequest request,
        Guid updatedByUserId,
        Guid? targetOrganizationId = null,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (workLocationId == Guid.Empty)
        {
            throw new ArgumentException("شناسه محل کار الزامی است.", nameof(workLocationId));
        }

        if (!Enum.IsDefined(typeof(QrCodeMode), request.Mode))
        {
            throw new ArgumentException("نوع کد کیوآر معتبر نمی‌باشد.");
        }

        WorkLocation? workLocation = await _repository.GetWorkLocationByIdAsync(workLocationId, cancellationToken);
        if (workLocation == null || workLocation.IsDeleted)
        {
            throw new KeyNotFoundException("محل کار مورد نظر یافت نشد.");
        }

        if (targetOrganizationId.HasValue &&
            targetOrganizationId.Value != Guid.Empty &&
            workLocation.OrganizationId != targetOrganizationId.Value)
        {
            throw new UnauthorizedAccessException("امکان تغییر تنظیمات کیوآر سازمان دیگر وجود ندارد.");
        }

        if (!workLocation.IsActive)
        {
            throw new InvalidOperationException("محل کار مورد نظر غیرفعال است.");
        }

        int fallbackMinutes = _options.DefaultRotationMinutes;
        if (fallbackMinutes < QrCodeOptions.MinRotationMinutes || fallbackMinutes > QrCodeOptions.MaxRotationMinutes)
        {
            fallbackMinutes = QrCodeOptions.DefaultRotationIntervalMinutes;
        }

        int rotationMinutes = request.RotationMinutes
            ?? (workLocation.QrCodeRotationMinutes > 0 ? workLocation.QrCodeRotationMinutes : fallbackMinutes);

        if (rotationMinutes < QrCodeOptions.MinRotationMinutes || rotationMinutes > QrCodeOptions.MaxRotationMinutes)
        {
            throw new ArgumentException(
                $"بازه چرخش کد کیوآر باید بین {QrCodeOptions.MinRotationMinutes} و {QrCodeOptions.MaxRotationMinutes} دقیقه باشد.");
        }

        bool modeChanged = workLocation.QrCodeMode != request.Mode;
        bool rotationChanged = workLocation.QrCodeRotationMinutes != rotationMinutes;

        workLocation.QrCodeVerificationEnabled = request.VerificationEnabled;
        workLocation.QrCodeMode = request.Mode;
        workLocation.QrCodeRotationMinutes = rotationMinutes;
        workLocation.UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        workLocation.UpdatedByUserId = updatedByUserId == Guid.Empty ? null : updatedByUserId;

        if (modeChanged || rotationChanged)
        {
            // Changing mode or the rotation interval invalidates every previously issued code.
            List<QRCode> activeQrCodes = await _repository.GetActiveQRCodesByWorkLocationIdAsync(workLocation.Id, cancellationToken);
            foreach (QRCode activeQrCode in activeQrCodes)
            {
                activeQrCode.IsActive = false;
            }
        }

        if (request.Mode == QrCodeMode.Dynamic)
        {
            if (string.IsNullOrWhiteSpace(workLocation.QrCodeReference))
            {
                workLocation.QrCodeReference = _tokenService.CreateReference();
            }

            if (string.IsNullOrWhiteSpace(workLocation.QrCodeSecret))
            {
                workLocation.QrCodeSecret = _tokenService.CreateSecret();
            }
        }

        await _repository.SaveChangesAsync(cancellationToken);

        QrCodeResponse? currentQrCode = null;

        if (workLocation.QrCodeVerificationEnabled)
        {
            currentQrCode = workLocation.QrCodeMode == QrCodeMode.Static
                ? await _qrCodeService.RegenerateAsync(workLocation.Id, updatedByUserId, targetOrganizationId, cancellationToken)
                : await _qrCodeService.GetCurrentAsync(workLocation.Id, targetOrganizationId, cancellationToken);
        }

        return QrCodeSettingsMapper.Map(workLocation, currentQrCode);
    }
}

using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Exceptions;
using AriaHR.Modules.Organization.Application.Options;
using AriaHR.Modules.Organization.Application.Repositories;
using AriaHR.Modules.Organization.Application.Services;
using AriaHR.Modules.Organization.Application.UseCases.CreateWorkLocation;
using AriaHR.Modules.Organization.Application.Validators;
using AriaHR.Modules.Organization.Domain.Entities;
using AriaHR.Modules.Organization.Domain.Enums;
using Microsoft.Extensions.Options;

namespace AriaHR.Modules.Organization.Application.UseCases.QrCode;

/// <summary>
/// Manages static and dynamic attendance QR codes for a work location.
/// Dynamic tokens are derived from the work location configuration and a server-side signing key, so rotating
/// them requires no database write.
/// </summary>
public class QrCodeManagementUseCase : IQrCodeManagementUseCase
{
    private readonly IWorkLocationRepository _workLocationRepository;
    private readonly IQrCodeRepository _qrCodeRepository;
    private readonly IQrCodeTokenService _qrCodeTokenService;
    private readonly QrCodeOptions _options;
    private readonly TimeProvider _timeProvider;

    public QrCodeManagementUseCase(
        IWorkLocationRepository workLocationRepository,
        IQrCodeRepository qrCodeRepository,
        IQrCodeTokenService qrCodeTokenService,
        IOptions<QrCodeOptions> options,
        TimeProvider? timeProvider = null)
    {
        _workLocationRepository = workLocationRepository ?? throw new ArgumentNullException(nameof(workLocationRepository));
        _qrCodeRepository = qrCodeRepository ?? throw new ArgumentNullException(nameof(qrCodeRepository));
        _qrCodeTokenService = qrCodeTokenService ?? throw new ArgumentNullException(nameof(qrCodeTokenService));
        _options = options?.Value ?? new QrCodeOptions();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    private DateTime NowUtc => _timeProvider.GetUtcNow().UtcDateTime;

    public async Task<QrCodeSettingsDto> GetSettingsAsync(
        Guid workLocationId,
        Guid? targetOrganizationId = null,
        CancellationToken cancellationToken = default)
    {
        var workLocation = await LoadAsync(workLocationId, targetOrganizationId, forUpdate: false, cancellationToken);
        return BuildSettings(workLocation);
    }

    public async Task<QrCodeSettingsDto> UpdateSettingsAsync(
        Guid workLocationId,
        UpdateQrCodeSettingsRequest request,
        Guid currentUserId,
        Guid? targetOrganizationId = null,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (!Enum.IsDefined(typeof(QrCodeMode), request.QrCodeMode))
        {
            throw new ArgumentException("حالت کد کیوآر معتبر نمی‌باشد.");
        }

        int rotationInterval = WorkLocationInputValidator.ValidateRotationInterval(request.QrRotationIntervalMinutes, _options);

        var workLocation = await LoadAsync(workLocationId, targetOrganizationId, forUpdate: true, cancellationToken);

        if (request.QrCodeMode == QrCodeMode.Static && string.IsNullOrWhiteSpace(workLocation.QrCodeIdentifier))
        {
            workLocation.QrCodeIdentifier = CreateWorkLocationUseCase.GenerateIdentifier();
        }

        workLocation.QrCodeMode = request.QrCodeMode;
        workLocation.QrRotationIntervalMinutes = rotationInterval;
        workLocation.QrCodeLastGeneratedAtUtc = NowUtc;
        Touch(workLocation, currentUserId);
        await _workLocationRepository.SaveChangesAsync(cancellationToken);

        return BuildSettings(workLocation);
    }

    public async Task<QrCodeResponse> GetCurrentAsync(
        Guid workLocationId,
        Guid? targetOrganizationId = null,
        CancellationToken cancellationToken = default)
    {
        var workLocation = await LoadAsync(workLocationId, targetOrganizationId, forUpdate: false, cancellationToken);
        EnsureEnabled(workLocation);
        var token = IssueToken(workLocation);
        return BuildResponse(workLocation, token);
    }

    public async Task<QrCodeResponse> GenerateAsync(
        Guid workLocationId,
        Guid currentUserId,
        Guid? targetOrganizationId = null,
        CancellationToken cancellationToken = default)
    {
        var workLocation = await LoadAsync(workLocationId, targetOrganizationId, forUpdate: true, cancellationToken);
        EnsureEnabled(workLocation);

        if (string.IsNullOrWhiteSpace(workLocation.QrCodeIdentifier))
        {
            workLocation.QrCodeIdentifier = CreateWorkLocationUseCase.GenerateIdentifier();
        }

        workLocation.QrCodeLastGeneratedAtUtc = NowUtc;
        Touch(workLocation, currentUserId);
        await _workLocationRepository.SaveChangesAsync(cancellationToken);

        var token = IssueToken(workLocation);
        return BuildResponse(workLocation, token);
    }

    public async Task<QrCodeSettingsDto> SetEnabledAsync(
        Guid workLocationId,
        bool enabled,
        Guid currentUserId,
        Guid? targetOrganizationId = null,
        CancellationToken cancellationToken = default)
    {
        var workLocation = await LoadAsync(workLocationId, targetOrganizationId, forUpdate: true, cancellationToken);

        if (enabled)
        {
            if (!workLocation.IsActive)
            {
                throw new QrCodeOperationException(
                    "WORK_LOCATION_INACTIVE",
                    "محل کار غیرفعال است و کد کیوآر آن قابل فعال‌سازی نیست.",
                    400);
            }

            if (string.IsNullOrWhiteSpace(workLocation.QrCodeIdentifier))
            {
                workLocation.QrCodeIdentifier = CreateWorkLocationUseCase.GenerateIdentifier();
            }
        }

        workLocation.QrCodeEnabled = enabled;
        if (enabled)
        {
            workLocation.QrCodeLastGeneratedAtUtc = NowUtc;
        }

        Touch(workLocation, currentUserId);
        await _workLocationRepository.SaveChangesAsync(cancellationToken);

        return BuildSettings(workLocation);
    }

    public async Task<QrCodeResponse> RevokeAsync(
        Guid workLocationId,
        Guid currentUserId,
        Guid? targetOrganizationId = null,
        CancellationToken cancellationToken = default)
    {
        var workLocation = await LoadAsync(workLocationId, targetOrganizationId, forUpdate: true, cancellationToken);

        workLocation.QrCodeIdentifier = CreateWorkLocationUseCase.GenerateIdentifier();
        workLocation.QrCodeLastGeneratedAtUtc = NowUtc;
        Touch(workLocation, currentUserId);

        var legacyQrCodes = await _qrCodeRepository.GetActiveQRCodesByWorkLocationIdAsync(workLocationId, cancellationToken);
        foreach (var legacy in legacyQrCodes)
        {
            legacy.IsActive = false;
            legacy.UpdatedAtUtc = NowUtc;
            legacy.UpdatedByUserId = currentUserId == Guid.Empty ? null : currentUserId;
        }

        await _workLocationRepository.SaveChangesAsync(cancellationToken);

        var token = IssueToken(workLocation);
        return BuildResponse(workLocation, token);
    }

    private async Task<WorkLocation> LoadAsync(
        Guid workLocationId,
        Guid? targetOrganizationId,
        bool forUpdate,
        CancellationToken cancellationToken)
    {
        if (workLocationId == Guid.Empty)
        {
            throw new ArgumentException("شناسه محل کار الزامی است.");
        }

        var workLocation = forUpdate
            ? await _workLocationRepository.GetForUpdateAsync(workLocationId, cancellationToken)
            : await _workLocationRepository.GetByIdAsync(workLocationId, cancellationToken);

        if (workLocation == null)
        {
            throw new QrCodeOperationException("WORK_LOCATION_NOT_FOUND", "محل کار مورد نظر یافت نشد.", 404);
        }

        if (targetOrganizationId.HasValue && targetOrganizationId.Value != Guid.Empty &&
            workLocation.OrganizationId != targetOrganizationId.Value)
        {
            throw new UnauthorizedAccessException("امکان دسترسی به کد کیوآر سازمان دیگر وجود ندارد.");
        }

        if (!workLocation.IsActive)
        {
            throw new QrCodeOperationException("WORK_LOCATION_INACTIVE", "محل کار مورد نظر غیرفعال است.", 400);
        }

        return workLocation;
    }

    private static void EnsureEnabled(WorkLocation workLocation)
    {
        if (!workLocation.QrCodeEnabled)
        {
            throw new QrCodeOperationException("QR_CODE_DISABLED", "کد کیوآر این محل کار غیرفعال است.", 400);
        }
    }

    private IssuedQrToken IssueToken(WorkLocation workLocation)
    {
        if (string.IsNullOrWhiteSpace(workLocation.QrCodeIdentifier))
        {
            throw new QrCodeOperationException(
                "QR_CODE_NOT_GENERATED",
                "کد کیوآر برای این محل کار تولید نشده است. ابتدا کد را تولید کنید.",
                400);
        }

        return _qrCodeTokenService.Issue(
            workLocation.Id,
            workLocation.QrCodeIdentifier,
            workLocation.QrCodeMode,
            workLocation.QrRotationIntervalMinutes,
            NowUtc);
    }

    private QrCodeResponse BuildResponse(WorkLocation workLocation, IssuedQrToken token)
    {
        return new QrCodeResponse
        {
            WorkLocationId = workLocation.Id,
            QrCodeMode = workLocation.QrCodeMode.ToString(),
            Token = token.Token,
            ExpiresAtUtc = token.ExpiresAtUtc,
            RefreshAfterUtc = token.RefreshAfterUtc,
            GeneratedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            QrCodeEnabled = workLocation.QrCodeEnabled,
            RotationIntervalMinutes = workLocation.QrRotationIntervalMinutes
        };
    }

    private QrCodeSettingsDto BuildSettings(WorkLocation workLocation)
    {
        return new QrCodeSettingsDto
        {
            WorkLocationId = workLocation.Id,
            QrCodeMode = workLocation.QrCodeMode.ToString(),
            QrCodeEnabled = workLocation.QrCodeEnabled,
            RotationIntervalMinutes = workLocation.QrRotationIntervalMinutes,
            MinRotationIntervalMinutes = _options.MinRotationIntervalMinutes,
            MaxRotationIntervalMinutes = _options.MaxRotationIntervalMinutes,
            DefaultRotationIntervalMinutes = _options.DefaultRotationIntervalMinutes,
            LastGeneratedAtUtc = workLocation.QrCodeLastGeneratedAtUtc,
            IsDynamic = workLocation.QrCodeMode == QrCodeMode.Dynamic
        };
    }

    private void Touch(WorkLocation workLocation, Guid currentUserId)
    {
        workLocation.UpdatedAtUtc = NowUtc;
        workLocation.UpdatedByUserId = currentUserId == Guid.Empty ? null : currentUserId;
    }
}

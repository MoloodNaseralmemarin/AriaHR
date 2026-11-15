using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Options;
using AriaHR.Modules.Organization.Application.Repositories;
using AriaHR.Modules.Organization.Domain.Entities;
using AriaHR.Modules.Organization.Domain.Enums;
using Microsoft.Extensions.Options;

namespace AriaHR.Modules.Organization.Application.Services;

public interface IQrCodeService
{
    Task<QrCodeResponse> GetCurrentAsync(
        Guid workLocationId,
        Guid? expectedOrganizationId = null,
        CancellationToken cancellationToken = default);

    Task<QrCodeResponse> RegenerateAsync(
        Guid workLocationId,
        Guid createdByUserId,
        Guid? expectedOrganizationId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Resolves the single server-side QR code for a work location. In dynamic mode the code is
/// derived deterministically from the work location secret and the current UTC time window,
/// so every device receives the same code until the window rotates.
/// </summary>
public class QrCodeService : IQrCodeService
{
    private readonly IQrCodeRepository _repository;
    private readonly IQrCodeTokenService _tokenService;
    private readonly QrCodeOptions _options;
    private readonly TimeProvider _timeProvider;

    public QrCodeService(
        IQrCodeRepository repository,
        IQrCodeTokenService tokenService,
        IOptions<QrCodeOptions> options,
        TimeProvider? timeProvider = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _options = options?.Value ?? new QrCodeOptions();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Task<QrCodeResponse> GetCurrentAsync(
        Guid workLocationId,
        Guid? expectedOrganizationId = null,
        CancellationToken cancellationToken = default)
    {
        return ResolveAsync(workLocationId, Guid.Empty, expectedOrganizationId, forceRegenerate: false, cancellationToken);
    }

    public Task<QrCodeResponse> RegenerateAsync(
        Guid workLocationId,
        Guid createdByUserId,
        Guid? expectedOrganizationId = null,
        CancellationToken cancellationToken = default)
    {
        return ResolveAsync(workLocationId, createdByUserId, expectedOrganizationId, forceRegenerate: true, cancellationToken);
    }

    private async Task<QrCodeResponse> ResolveAsync(
        Guid workLocationId,
        Guid createdByUserId,
        Guid? expectedOrganizationId,
        bool forceRegenerate,
        CancellationToken cancellationToken)
    {
        if (workLocationId == Guid.Empty)
        {
            throw new ArgumentException("شناسه محل کار الزامی است.", nameof(workLocationId));
        }

        WorkLocation? workLocation = await _repository.GetWorkLocationByIdAsync(workLocationId, cancellationToken);
        if (workLocation == null || workLocation.IsDeleted)
        {
            throw new KeyNotFoundException("محل کار مورد نظر یافت نشد.");
        }

        if (expectedOrganizationId.HasValue &&
            expectedOrganizationId.Value != Guid.Empty &&
            workLocation.OrganizationId != expectedOrganizationId.Value)
        {
            throw new UnauthorizedAccessException("امکان دسترسی به کد کیوآر سازمان دیگر وجود ندارد.");
        }

        if (!workLocation.IsActive)
        {
            throw new InvalidOperationException("محل کار مورد نظر غیرفعال است.");
        }

        DateTime nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        if (!workLocation.QrCodeVerificationEnabled)
        {
            if (forceRegenerate)
            {
                throw new InvalidOperationException("تأیید کد کیوآر برای این محل کار غیرفعال است.");
            }

            return new QrCodeResponse
            {
                QrCodeId = null,
                WorkLocationId = workLocation.Id,
                OrganizationId = workLocation.OrganizationId,
                Code = null,
                Mode = workLocation.QrCodeMode.ToString(),
                VerificationEnabled = false,
                GeneratedAtUtc = null,
                ExpiresAtUtc = null,
                RemainingValiditySeconds = null
            };
        }

        if (workLocation.QrCodeMode == QrCodeMode.Dynamic)
        {
            return await BuildDynamicAsync(workLocation, createdByUserId, nowUtc, cancellationToken);
        }

        return await BuildStaticAsync(workLocation, createdByUserId, nowUtc, forceRegenerate, cancellationToken);
    }

    private async Task<QrCodeResponse> BuildDynamicAsync(
        WorkLocation workLocation,
        Guid createdByUserId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        int rotationMinutes = ResolveRotationMinutes(workLocation.QrCodeRotationMinutes);

        if (workLocation.QrCodeRotationMinutes != rotationMinutes)
        {
            workLocation.QrCodeRotationMinutes = rotationMinutes;
        }

        if (string.IsNullOrWhiteSpace(workLocation.QrCodeReference))
        {
            workLocation.QrCodeReference = _tokenService.CreateReference();
        }

        if (string.IsNullOrWhiteSpace(workLocation.QrCodeSecret))
        {
            workLocation.QrCodeSecret = _tokenService.CreateSecret();
        }

        QrCodeWindow window = _tokenService.GetWindow(nowUtc, rotationMinutes);
        string code = _tokenService.CreateDynamicToken(
            workLocation.QrCodeReference!,
            workLocation.QrCodeSecret!,
            window);

        QRCode? qrCode = await _repository.GetQrCodeForWindowAsync(workLocation.Id, window.StartsAtUtc, cancellationToken);

        if (qrCode == null)
        {
            List<QRCode> activeQrCodes = await _repository.GetActiveQRCodesByWorkLocationIdAsync(workLocation.Id, cancellationToken);
            foreach (QRCode activeQrCode in activeQrCodes)
            {
                activeQrCode.IsActive = false;
            }

            qrCode = new QRCode
            {
                Id = Guid.NewGuid(),
                OrganizationId = workLocation.OrganizationId,
                WorkLocationId = workLocation.Id,
                Code = code,
                Title = "Dynamic Attendance QR",
                ValidFrom = window.StartsAtUtc,
                ValidTo = window.EndsAtUtc,
                IsActive = true,
                CreatedBy = createdByUserId,
                CreatedAtUtc = nowUtc,
                CreatedByUserId = createdByUserId == Guid.Empty ? null : createdByUserId
            };

            await _repository.AddAsync(qrCode, cancellationToken);
        }
        else
        {
            qrCode.Code = code;
            qrCode.ValidFrom = window.StartsAtUtc;
            qrCode.ValidTo = window.EndsAtUtc;
            qrCode.IsActive = true;
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return new QrCodeResponse
        {
            QrCodeId = qrCode.Id,
            WorkLocationId = workLocation.Id,
            OrganizationId = workLocation.OrganizationId,
            Code = code,
            Mode = QrCodeMode.Dynamic.ToString(),
            VerificationEnabled = true,
            GeneratedAtUtc = window.StartsAtUtc,
            ExpiresAtUtc = window.EndsAtUtc,
            RemainingValiditySeconds = Math.Max(0, (int)Math.Ceiling((window.EndsAtUtc - nowUtc).TotalSeconds))
        };
    }

    private async Task<QrCodeResponse> BuildStaticAsync(
        WorkLocation workLocation,
        Guid createdByUserId,
        DateTime nowUtc,
        bool forceRegenerate,
        CancellationToken cancellationToken)
    {
        QRCode? activeStaticQrCode = await _repository.GetActiveStaticQrCodeAsync(workLocation.Id, cancellationToken);

        if (activeStaticQrCode == null || forceRegenerate)
        {
            List<QRCode> activeQrCodes = await _repository.GetActiveQRCodesByWorkLocationIdAsync(workLocation.Id, cancellationToken);
            foreach (QRCode activeQrCode in activeQrCodes)
            {
                activeQrCode.IsActive = false;
            }

            activeStaticQrCode = new QRCode
            {
                Id = Guid.NewGuid(),
                OrganizationId = workLocation.OrganizationId,
                WorkLocationId = workLocation.Id,
                Code = _tokenService.CreateStaticToken(),
                Title = "Static Attendance QR",
                ValidFrom = nowUtc,
                ValidTo = null,
                IsActive = true,
                CreatedBy = createdByUserId,
                CreatedAtUtc = nowUtc,
                CreatedByUserId = createdByUserId == Guid.Empty ? null : createdByUserId
            };

            await _repository.AddAsync(activeStaticQrCode, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
        }

        return new QrCodeResponse
        {
            QrCodeId = activeStaticQrCode.Id,
            WorkLocationId = workLocation.Id,
            OrganizationId = workLocation.OrganizationId,
            Code = activeStaticQrCode.Code,
            Mode = QrCodeMode.Static.ToString(),
            VerificationEnabled = true,
            GeneratedAtUtc = activeStaticQrCode.ValidFrom,
            ExpiresAtUtc = null,
            RemainingValiditySeconds = null
        };
    }

    private int ResolveRotationMinutes(int configuredMinutes)
    {
        int fallback = _options.DefaultRotationMinutes;
        if (fallback < QrCodeOptions.MinRotationMinutes || fallback > QrCodeOptions.MaxRotationMinutes)
        {
            fallback = QrCodeOptions.DefaultRotationIntervalMinutes;
        }

        if (configuredMinutes < QrCodeOptions.MinRotationMinutes || configuredMinutes > QrCodeOptions.MaxRotationMinutes)
        {
            return fallback;
        }

        return configuredMinutes;
    }
}

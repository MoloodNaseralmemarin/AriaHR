using System.Security.Cryptography;
using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Options;
using AriaHR.Modules.Organization.Application.Repositories;
using AriaHR.Modules.Organization.Domain.Entities;
using Microsoft.Extensions.Options;

namespace AriaHR.Modules.Organization.Application.UseCases.GenerateQrCode;

public class GenerateQrCodeUseCase : IGenerateQrCodeUseCase
{
    private readonly IQrCodeRepository _qrCodeRepository;
    private readonly QrCodeOptions _options;

    public GenerateQrCodeUseCase(
        IQrCodeRepository qrCodeRepository,
        IOptions<QrCodeOptions> options)
    {
        _qrCodeRepository = qrCodeRepository ?? throw new ArgumentNullException(nameof(qrCodeRepository));
        _options = options?.Value ?? new QrCodeOptions();
    }

    public async Task<QrCodeResponse> ExecuteAsync(
        Guid workLocationId,
        Guid createdByUserId,
        Guid? targetOrganizationId = null,
        CancellationToken cancellationToken = default)
    {
        if (workLocationId == Guid.Empty)
        {
            throw new ArgumentException("شناسه محل کار الزامی است.", nameof(workLocationId));
        }

        var workLocation = await _qrCodeRepository.GetWorkLocationByIdAsync(workLocationId, cancellationToken);
        if (workLocation == null || workLocation.IsDeleted)
        {
            throw new KeyNotFoundException("محل کار مورد نظر یافت نشد.");
        }

        if (targetOrganizationId.HasValue && targetOrganizationId.Value != Guid.Empty && workLocation.OrganizationId != targetOrganizationId.Value)
        {
            throw new UnauthorizedAccessException("امکان ایجاد کد کیوآر برای سازمان دیگر وجود ندارد.");
        }

        if (!workLocation.IsActive)
        {
            throw new InvalidOperationException("محل کار مورد نظر غیرفعال است.");
        }

        var activeQRCodes = await _qrCodeRepository.GetActiveQRCodesByWorkLocationIdAsync(workLocationId, cancellationToken);
        foreach (var activeQR in activeQRCodes)
        {
            activeQR.IsActive = false;
        }

        byte[] tokenBytes = RandomNumberGenerator.GetBytes(32);
        string secureToken = Convert.ToHexStringLower(tokenBytes);

        int expirationMinutes = _options.ExpirationMinutes > 0 ? _options.ExpirationMinutes : 5;
        DateTime nowUtc = DateTime.UtcNow;
        DateTime expiresAtUtc = nowUtc.AddMinutes(expirationMinutes);

        var qrCode = new QRCode
        {
            Id = Guid.NewGuid(),
            OrganizationId = workLocation.OrganizationId,
            WorkLocationId = workLocation.Id,
            Code = secureToken,
            Title = "Dynamic Attendance QR",
            ValidFrom = nowUtc,
            ValidTo = expiresAtUtc,
            IsActive = true,
            CreatedBy = createdByUserId,
            CreatedAtUtc = nowUtc,
            CreatedByUserId = createdByUserId
        };

        await _qrCodeRepository.AddAsync(qrCode, cancellationToken);
        await _qrCodeRepository.SaveChangesAsync(cancellationToken);

        return new QrCodeResponse
        {
            Code = qrCode.Code,
            ExpiresAtUtc = expiresAtUtc
        };
    }
}

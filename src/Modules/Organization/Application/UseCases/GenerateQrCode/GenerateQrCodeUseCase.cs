using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Services;

namespace AriaHR.Modules.Organization.Application.UseCases.GenerateQrCode;

/// <summary>
/// Manual generation endpoint. Static work locations receive a brand new code and the previous
/// code is invalidated. Dynamic work locations return the code for the current server-side window.
/// </summary>
public class GenerateQrCodeUseCase : IGenerateQrCodeUseCase
{
    private readonly IQrCodeService _qrCodeService;

    public GenerateQrCodeUseCase(IQrCodeService qrCodeService)
    {
        _qrCodeService = qrCodeService ?? throw new ArgumentNullException(nameof(qrCodeService));
    }

    public Task<QrCodeResponse> ExecuteAsync(
        Guid workLocationId,
        Guid createdByUserId,
        Guid? targetOrganizationId = null,
        CancellationToken cancellationToken = default)
    {
        return _qrCodeService.RegenerateAsync(
            workLocationId,
            createdByUserId,
            targetOrganizationId,
            cancellationToken);
    }
}

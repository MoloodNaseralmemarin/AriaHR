using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Services;

namespace AriaHR.Modules.Organization.Application.UseCases.GetQrCode;

public class GetQrCodeUseCase : IGetQrCodeUseCase
{
    private readonly IQrCodeService _qrCodeService;

    public GetQrCodeUseCase(IQrCodeService qrCodeService)
    {
        _qrCodeService = qrCodeService ?? throw new ArgumentNullException(nameof(qrCodeService));
    }

    public Task<QrCodeResponse> ExecuteAsync(
        Guid workLocationId,
        Guid? targetOrganizationId = null,
        CancellationToken cancellationToken = default)
    {
        return _qrCodeService.GetCurrentAsync(workLocationId, targetOrganizationId, cancellationToken);
    }
}

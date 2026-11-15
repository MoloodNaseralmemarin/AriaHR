using AriaHR.Modules.Organization.Application.DTOs;

namespace AriaHR.Modules.Organization.Application.UseCases.GetQrCode;

public interface IGetQrCodeUseCase
{
    Task<QrCodeResponse> ExecuteAsync(
        Guid workLocationId,
        Guid? targetOrganizationId = null,
        CancellationToken cancellationToken = default);
}

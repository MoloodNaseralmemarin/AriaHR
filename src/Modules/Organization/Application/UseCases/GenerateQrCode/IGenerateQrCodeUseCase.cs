using AriaHR.Modules.Organization.Application.DTOs;

namespace AriaHR.Modules.Organization.Application.UseCases.GenerateQrCode;

public interface IGenerateQrCodeUseCase
{
    Task<QrCodeResponse> ExecuteAsync(
        Guid workLocationId,
        Guid createdByUserId,
        Guid? targetOrganizationId = null,
        CancellationToken cancellationToken = default);
}

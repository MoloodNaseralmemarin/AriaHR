using AriaHR.Modules.Organization.Application.DTOs;

namespace AriaHR.Modules.Organization.Application.UseCases.GetQrCodeSettings;

public interface IGetQrCodeSettingsUseCase
{
    Task<QrCodeSettingsResponse> ExecuteAsync(
        Guid workLocationId,
        Guid? targetOrganizationId = null,
        CancellationToken cancellationToken = default);
}

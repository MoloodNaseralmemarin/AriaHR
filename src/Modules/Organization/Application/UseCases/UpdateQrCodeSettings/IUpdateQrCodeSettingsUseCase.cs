using AriaHR.Modules.Organization.Application.DTOs;

namespace AriaHR.Modules.Organization.Application.UseCases.UpdateQrCodeSettings;

public interface IUpdateQrCodeSettingsUseCase
{
    Task<QrCodeSettingsResponse> ExecuteAsync(
        Guid workLocationId,
        UpdateQrCodeSettingsRequest request,
        Guid updatedByUserId,
        Guid? targetOrganizationId = null,
        CancellationToken cancellationToken = default);
}

using AriaHR.Modules.Organization.Application.DTOs;

namespace AriaHR.Modules.Organization.Application.UseCases.QrCode;

public interface IQrCodeManagementUseCase
{
    Task<QrCodeSettingsDto> GetSettingsAsync(
        Guid workLocationId,
        Guid? targetOrganizationId = null,
        CancellationToken cancellationToken = default);

    Task<QrCodeSettingsDto> UpdateSettingsAsync(
        Guid workLocationId,
        UpdateQrCodeSettingsRequest request,
        Guid currentUserId,
        Guid? targetOrganizationId = null,
        CancellationToken cancellationToken = default);

    Task<QrCodeResponse> GetCurrentAsync(
        Guid workLocationId,
        Guid? targetOrganizationId = null,
        CancellationToken cancellationToken = default);

    Task<QrCodeResponse> GenerateAsync(
        Guid workLocationId,
        Guid currentUserId,
        Guid? targetOrganizationId = null,
        CancellationToken cancellationToken = default);

    Task<QrCodeSettingsDto> SetEnabledAsync(
        Guid workLocationId,
        bool enabled,
        Guid currentUserId,
        Guid? targetOrganizationId = null,
        CancellationToken cancellationToken = default);

    Task<QrCodeResponse> RevokeAsync(
        Guid workLocationId,
        Guid currentUserId,
        Guid? targetOrganizationId = null,
        CancellationToken cancellationToken = default);
}

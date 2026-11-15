namespace AriaHR.Shared.Services;

public record WorkLocationDetailsDto(
    Guid Id,
    Guid OrganizationId,
    string Name,
    decimal Latitude,
    decimal Longitude,
    decimal AllowedRadiusInMeters,
    bool IsActive,
    bool QrCodeEnabled,
    int QrRotationIntervalMinutes
);

public record QrCodeValidationResultDto(
    bool IsValid,
    string? ErrorCode,
    Guid? QRCodeId,
    Guid? WorkLocationId,
    Guid? OrganizationId,
    string? QrCodeMode,
    string? ErrorMessage
);

public interface IWorkLocationLookupService
{
    Task<WorkLocationDetailsDto?> GetWorkLocationByIdAsync(Guid workLocationId, CancellationToken cancellationToken = default);

    Task<WorkLocationDetailsDto?> GetWorkLocationForOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);

    Task<QrCodeValidationResultDto> ValidateQrCodeAsync(
        string qrCodeToken,
        Guid? expectedOrganizationId = null,
        DateTime? nowUtc = null,
        CancellationToken cancellationToken = default);
}

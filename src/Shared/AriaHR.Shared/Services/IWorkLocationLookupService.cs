namespace AriaHR.Shared.Services;

public record WorkLocationDetailsDto(
    Guid Id,
    Guid OrganizationId,
    double Latitude,
    double Longitude,
    double RadiusInMeters,
    bool IsActive
);

public record QrCodeValidationResultDto(
    bool IsValid,
    Guid? QRCodeId,
    Guid? WorkLocationId,
    Guid? OrganizationId,
    string? ErrorMessage
);

public interface IWorkLocationLookupService
{
    Task<WorkLocationDetailsDto?> GetWorkLocationByIdAsync(Guid workLocationId, CancellationToken cancellationToken = default);
    Task<WorkLocationDetailsDto?> GetWorkLocationForOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<QrCodeValidationResultDto> ValidateQrCodeAsync(string qrCodeToken, Guid? expectedOrganizationId = null, CancellationToken cancellationToken = default);
}

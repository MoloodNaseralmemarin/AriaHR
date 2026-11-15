using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Options;
using AriaHR.Modules.Organization.Domain.Entities;

namespace AriaHR.Modules.Organization.Application.Services;

internal static class QrCodeSettingsMapper
{
    public static QrCodeSettingsResponse Map(WorkLocation workLocation, QrCodeResponse? currentQrCode)
    {
        return new QrCodeSettingsResponse
        {
            WorkLocationId = workLocation.Id,
            OrganizationId = workLocation.OrganizationId,
            VerificationEnabled = workLocation.QrCodeVerificationEnabled,
            Mode = workLocation.QrCodeMode.ToString(),
            RotationMinutes = workLocation.QrCodeRotationMinutes,
            PredefinedRotationMinutes = QrCodeOptions.PredefinedRotationMinutes,
            MinRotationMinutes = QrCodeOptions.MinRotationMinutes,
            MaxRotationMinutes = QrCodeOptions.MaxRotationMinutes,
            CurrentCode = currentQrCode?.Code,
            ExpiresAtUtc = currentQrCode?.ExpiresAtUtc,
            RemainingValiditySeconds = currentQrCode?.RemainingValiditySeconds
        };
    }
}

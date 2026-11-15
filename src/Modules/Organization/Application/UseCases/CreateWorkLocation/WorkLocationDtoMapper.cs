namespace AriaHR.Modules.Organization.Application.UseCases.CreateWorkLocation;

using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Domain.Entities;

internal static class WorkLocationDtoMapper
{
    public static WorkLocationDto Map(WorkLocation location)
    {
        return new WorkLocationDto
        {
            Id = location.Id,
            OrganizationId = location.OrganizationId,
            Name = location.Name,
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            AllowedRadiusInMeters = location.AllowedRadiusInMeters,
            IsActive = location.IsActive,
            QrCodeMode = location.QrCodeMode.ToString(),
            QrCodeEnabled = location.QrCodeEnabled,
            QrRotationIntervalMinutes = location.QrRotationIntervalMinutes,
            QrCodeLastGeneratedAtUtc = location.QrCodeLastGeneratedAtUtc,
            CreatedAtUtc = location.CreatedAtUtc,
            CreatedByUserId = location.CreatedByUserId,
            UpdatedAtUtc = location.UpdatedAtUtc,
            UpdatedByUserId = location.UpdatedByUserId
        };
    }
}

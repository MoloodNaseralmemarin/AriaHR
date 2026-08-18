using AriaHR.Shared;

namespace AriaHR.Modules.Requests.Domain.Entities;

/// <summary>
/// MissionLocationLog entity storing location logs captured during official mission travel.
/// </summary>
public class MissionLocationLog : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Guid MissionRequestId { get; set; }
    public MissionRequest? MissionRequest { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Accuracy { get; set; }
    public DateTime RecordedAt { get; set; }
    public string? DeviceInfo { get; set; }
}

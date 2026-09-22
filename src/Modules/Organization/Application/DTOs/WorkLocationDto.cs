namespace AriaHR.Modules.Organization.Application.DTOs;

public class WorkLocationDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double RadiusInMeters { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
}

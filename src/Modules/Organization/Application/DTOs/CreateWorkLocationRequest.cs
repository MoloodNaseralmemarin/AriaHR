namespace AriaHR.Modules.Organization.Application.DTOs;

public class CreateWorkLocationRequest
{
    public Guid? OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double RadiusInMeters { get; set; }
    public bool IsActive { get; set; } = true;
}

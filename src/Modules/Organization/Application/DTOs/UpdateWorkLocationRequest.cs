namespace AriaHR.Modules.Organization.Application.DTOs;

public class UpdateWorkLocationRequest
{
    public string? Name { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal AllowedRadiusInMeters { get; set; }
    public bool IsActive { get; set; } = true;
}

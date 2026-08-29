namespace AriaHR.Modules.Organization.Application.DTOs;

public class RecentOrganizationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

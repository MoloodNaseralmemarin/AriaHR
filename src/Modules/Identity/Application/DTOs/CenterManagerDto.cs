namespace AriaHR.Modules.Identity.Application.DTOs;

public class CenterManagerDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Gender { get; set; }
    public Guid? OrganizationId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

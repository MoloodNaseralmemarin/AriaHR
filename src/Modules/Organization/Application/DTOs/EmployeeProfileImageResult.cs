namespace AriaHR.Modules.Organization.Application.DTOs;

public class EmployeeProfileImageResult
{
    public Guid EmployeeId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public byte[] ImageBytes { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "image/jpeg";
    public string? FileName { get; set; }
}

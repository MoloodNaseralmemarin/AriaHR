namespace AriaHR.Modules.Requests.Application.DTOs;

public class LeaveCategoryDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MaxDaysPerYear { get; set; }
    public bool IsPaid { get; set; }
    public bool RequiresAttachment { get; set; }
    public bool IsActive { get; set; }
}

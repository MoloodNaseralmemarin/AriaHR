namespace AriaHR.Modules.Requests.Application.DTOs;

public class LeaveCategoryListItemDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

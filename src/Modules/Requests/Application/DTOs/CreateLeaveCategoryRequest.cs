namespace AriaHR.Modules.Requests.Application.DTOs;

public class CreateLeaveCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public int? MaxDaysPerYear { get; set; }
    public bool IsPaid { get; set; } = true;
    public bool RequiresAttachment { get; set; } = false;
    public Guid? OrganizationId { get; set; }
}

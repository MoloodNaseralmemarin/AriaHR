namespace AriaHR.Modules.Requests.Application.DTOs;

public class UpdateLeaveCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public int? MaxDaysPerYear { get; set; }
    public bool IsPaid { get; set; } = true;
    public bool RequiresAttachment { get; set; } = false;
}

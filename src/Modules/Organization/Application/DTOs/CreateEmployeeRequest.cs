namespace AriaHR.Modules.Organization.Application.DTOs;

public class CreateEmployeeRequest
{
    public Guid UserId { get; set; }
    public string PersonnelCode { get; set; } = string.Empty;
    public string NationalCode { get; set; } = string.Empty;
    public DateOnly BirthDate { get; set; }
    public DateOnly HireDate { get; set; }
    public string? Gender { get; set; }
    public string? ProfileImagePath { get; set; }
    public Guid? OrganizationId { get; set; }
}

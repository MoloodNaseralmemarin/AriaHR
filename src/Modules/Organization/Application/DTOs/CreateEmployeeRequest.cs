namespace AriaHR.Modules.Organization.Application.DTOs;

public class CreateEmployeeRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PersonnelCode { get; set; } = string.Empty;
    public string NationalCode { get; set; } = string.Empty;
    public DateOnly BirthDate { get; set; }
    public DateOnly HireDate { get; set; }
    public string? Gender { get; set; }
    public string? ProfileImagePath { get; set; }
    public Guid? OrganizationId { get; set; }
}

using System.ComponentModel.DataAnnotations;
using AriaHR.Modules.Organization.Domain.Enums;

namespace AriaHR.Modules.Organization.Application.DTOs;

public class CreateOrganizationRequest
{
    [Required(ErrorMessage = "نام سازمان الزامی است.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "کد سازمان الزامی است.")]
    public string Code { get; set; } = string.Empty;

    public OrganizationType Type { get; set; }
    public string? NationalIdentifier { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }

    [Required(ErrorMessage = "نام مدیر الزامی است.")]
    public string? ManagerFirstName { get; set; }

    [Required(ErrorMessage = "نام خانوادگی مدیر الزامی است.")]
    public string? ManagerLastName { get; set; }

    [Required(ErrorMessage = "شماره موبایل مدیر الزامی است.")]
    public string? ManagerMobile { get; set; }

    public bool IsActive { get; set; } = true;
}

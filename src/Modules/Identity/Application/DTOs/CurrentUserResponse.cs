namespace AriaHR.Modules.Identity.Application.DTOs;

public record CurrentUserResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string PhoneNumber,
    List<string> Roles,
    string OrganizationName = "");

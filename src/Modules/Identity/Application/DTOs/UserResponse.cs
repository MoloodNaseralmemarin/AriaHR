namespace AriaHR.Modules.Identity.Application.DTOs;

public record UserResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string PhoneNumber,
    List<string> Roles);

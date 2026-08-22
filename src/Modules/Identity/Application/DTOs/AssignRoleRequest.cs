namespace AriaHR.Modules.Identity.Application.DTOs;

public record AssignRoleRequest(Guid UserId, string RoleName);

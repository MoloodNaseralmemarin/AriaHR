using AriaHR.Modules.Organization.Application.DTOs;

namespace AriaHR.Modules.Organization.Application.Services;

public interface IEmployeeIdentityService
{
    Task<EmployeeDto> CreateEmployeeWithUserAsync(
        CreateEmployeeRequest request,
        Guid organizationId,
        Guid createdByUserId,
        CancellationToken cancellationToken = default);
}

using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Services;

namespace AriaHR.Modules.Organization.Application.UseCases.CreateEmployee;

public class CreateEmployeeUseCase : ICreateEmployeeUseCase
{
    private readonly IEmployeeIdentityService _employeeIdentityService;

    public CreateEmployeeUseCase(IEmployeeIdentityService employeeIdentityService)
    {
        _employeeIdentityService = employeeIdentityService ?? throw new ArgumentNullException(nameof(employeeIdentityService));
    }

    public Task<EmployeeDto> ExecuteAsync(
        CreateEmployeeRequest request,
        Guid organizationId,
        Guid createdByUserId,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return _employeeIdentityService.CreateEmployeeWithUserAsync(
            request,
            organizationId,
            createdByUserId,
            cancellationToken);
    }
}

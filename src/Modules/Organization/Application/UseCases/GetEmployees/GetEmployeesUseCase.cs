using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Services;

namespace AriaHR.Modules.Organization.Application.UseCases.GetEmployees;

public class GetEmployeesUseCase : IGetEmployeesUseCase
{
    private readonly IEmployeeIdentityService _employeeIdentityService;

    public GetEmployeesUseCase(IEmployeeIdentityService employeeIdentityService)
    {
        _employeeIdentityService = employeeIdentityService ?? throw new ArgumentNullException(nameof(employeeIdentityService));
    }

    public Task<IEnumerable<EmployeeDto>> ExecuteAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("شناسه سازمان الزامی است.", nameof(organizationId));
        }

        return _employeeIdentityService.GetEmployeesByOrganizationAsync(
            organizationId,
            cancellationToken);
    }
}

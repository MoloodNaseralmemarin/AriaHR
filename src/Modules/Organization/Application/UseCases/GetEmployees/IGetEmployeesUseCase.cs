using AriaHR.Modules.Organization.Application.DTOs;

namespace AriaHR.Modules.Organization.Application.UseCases.GetEmployees;

public interface IGetEmployeesUseCase
{
    Task<IEnumerable<EmployeeDto>> ExecuteAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);
}

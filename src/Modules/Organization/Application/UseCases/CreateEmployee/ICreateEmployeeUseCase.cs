using AriaHR.Modules.Organization.Application.DTOs;

namespace AriaHR.Modules.Organization.Application.UseCases.CreateEmployee;

public interface ICreateEmployeeUseCase
{
    Task<EmployeeDto> ExecuteAsync(
        CreateEmployeeRequest request,
        Guid organizationId,
        Guid createdByUserId,
        CancellationToken cancellationToken = default);
}

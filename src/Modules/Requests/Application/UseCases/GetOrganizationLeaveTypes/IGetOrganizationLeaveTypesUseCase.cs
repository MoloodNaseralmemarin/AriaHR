using AriaHR.Modules.Requests.Application.DTOs;

namespace AriaHR.Modules.Requests.Application.UseCases.GetOrganizationLeaveTypes;

public interface IGetOrganizationLeaveTypesUseCase
{
    Task<IEnumerable<LeaveTypeResponse>> ExecuteAsync(Guid organizationId, CancellationToken cancellationToken = default);
}

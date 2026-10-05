using AriaHR.Modules.Requests.Application.DTOs;

namespace AriaHR.Modules.Requests.Application.UseCases.GetOrganizationLeaveRequests;

public interface IGetOrganizationLeaveRequestsUseCase
{
    Task<IEnumerable<LeaveRequestDto>> ExecuteAsync(
        Guid organizationId,
        GetOrganizationRequestsQuery? query,
        CancellationToken cancellationToken = default);
}

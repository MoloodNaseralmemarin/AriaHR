using AriaHR.Modules.Requests.Application.DTOs;
using AriaHR.Modules.Requests.Application.Repositories;

namespace AriaHR.Modules.Requests.Application.UseCases.GetOrganizationLeaveRequests;

public class GetOrganizationLeaveRequestsUseCase : IGetOrganizationLeaveRequestsUseCase
{
    private readonly ILeaveRequestRepository _repository;

    public GetOrganizationLeaveRequestsUseCase(ILeaveRequestRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<IEnumerable<LeaveRequestDto>> ExecuteAsync(
        Guid organizationId,
        GetOrganizationRequestsQuery? query,
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("شناسه سازمان الزامی است.", nameof(organizationId));
        }

        var requests = await _repository.GetOrganizationRequestsAsync(
            organizationId,
            query?.Status,
            query?.EmployeeId,
            query?.From,
            query?.To,
            cancellationToken);

        return requests.Select(r => new LeaveRequestDto
        {
            Id = r.Id,
            EmployeeId = r.EmployeeId,
            OrganizationId = r.OrganizationId,
            LeaveType = r.LeaveType,
            Date = r.Date,
            StartTime = r.StartTime,
            EndTime = r.EndTime,
            Reason = r.Reason,
            Status = r.Status,
            RejectedReason = r.RejectedReason,
            ApprovedAt = r.ApprovedAt,
            ApprovedBy = r.ApprovedBy,
            CreatedAtUtc = r.CreatedAtUtc
        });
    }
}

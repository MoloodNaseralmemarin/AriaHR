using AriaHR.Modules.Requests.Application.DTOs;
using AriaHR.Modules.Requests.Application.Repositories;

namespace AriaHR.Modules.Requests.Application.UseCases.GetOrganizationLeaveTypes;

public class GetOrganizationLeaveTypesUseCase : IGetOrganizationLeaveTypesUseCase
{
    private readonly ILeaveTypeRepository _leaveTypeRepository;

    public GetOrganizationLeaveTypesUseCase(ILeaveTypeRepository leaveTypeRepository)
    {
        _leaveTypeRepository = leaveTypeRepository ?? throw new ArgumentNullException(nameof(leaveTypeRepository));
    }

    public async Task<IEnumerable<LeaveTypeResponse>> ExecuteAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("شناسه سازمان الزامی است.", nameof(organizationId));
        }

        var list = await _leaveTypeRepository.GetAllByOrganizationIdAsync(organizationId, cancellationToken);

        return list.Select(leaveType => new LeaveTypeResponse
        {
            Id = leaveType.Id,
            Name = leaveType.Name,
            MaxDaysPerYear = leaveType.MaxDaysPerYear,
            IsPaid = leaveType.IsPaid,
            RequiresAttachment = leaveType.RequiresAttachment,
            IsActive = leaveType.IsActive
        });
    }
}

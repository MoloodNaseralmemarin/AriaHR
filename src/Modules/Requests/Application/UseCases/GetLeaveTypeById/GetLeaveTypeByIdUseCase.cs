using AriaHR.Modules.Requests.Application.DTOs;
using AriaHR.Modules.Requests.Application.Repositories;

namespace AriaHR.Modules.Requests.Application.UseCases.GetLeaveTypeById;

public class GetLeaveTypeByIdUseCase : IGetLeaveTypeByIdUseCase
{
    private readonly ILeaveTypeRepository _leaveTypeRepository;

    public GetLeaveTypeByIdUseCase(ILeaveTypeRepository leaveTypeRepository)
    {
        _leaveTypeRepository = leaveTypeRepository ?? throw new ArgumentNullException(nameof(leaveTypeRepository));
    }

    public async Task<LeaveTypeResponse?> ExecuteAsync(
        Guid id,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty || organizationId == Guid.Empty)
        {
            return null;
        }

        var leaveType = await _leaveTypeRepository.GetByIdAndOrganizationIdAsync(id, organizationId, cancellationToken);
        if (leaveType == null)
        {
            return null;
        }

        return new LeaveTypeResponse
        {
            Id = leaveType.Id,
            Name = leaveType.Name,
            MaxDaysPerYear = leaveType.MaxDaysPerYear,
            IsPaid = leaveType.IsPaid,
            RequiresAttachment = leaveType.RequiresAttachment,
            IsActive = leaveType.IsActive
        };
    }
}

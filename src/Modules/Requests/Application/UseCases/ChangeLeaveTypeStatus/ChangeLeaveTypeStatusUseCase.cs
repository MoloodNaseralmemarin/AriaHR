using AriaHR.Modules.Requests.Application.DTOs;
using AriaHR.Modules.Requests.Application.Repositories;

namespace AriaHR.Modules.Requests.Application.UseCases.ChangeLeaveTypeStatus;

public class ChangeLeaveTypeStatusUseCase : IChangeLeaveTypeStatusUseCase
{
    private readonly ILeaveTypeRepository _leaveTypeRepository;

    public ChangeLeaveTypeStatusUseCase(ILeaveTypeRepository leaveTypeRepository)
    {
        _leaveTypeRepository = leaveTypeRepository ?? throw new ArgumentNullException(nameof(leaveTypeRepository));
    }

    public async Task<LeaveTypeResponse> ExecuteAsync(
        Guid id,
        ChangeLeaveTypeStatusRequest request,
        Guid organizationId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (id == Guid.Empty)
        {
            throw new ArgumentException("شناسه نوع مرخصی الزامی است.", nameof(id));
        }

        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("شناسه سازمان الزامی است.", nameof(organizationId));
        }

        var leaveType = await _leaveTypeRepository.GetByIdAndOrganizationIdAsync(id, organizationId, cancellationToken);
        if (leaveType == null)
        {
            throw new KeyNotFoundException("نوع مرخصی مورد نظر یافت نشد.");
        }

        leaveType.IsActive = request.IsActive;
        leaveType.UpdatedAtUtc = DateTime.UtcNow;
        leaveType.UpdatedByUserId = currentUserId;

        await _leaveTypeRepository.UpdateAsync(leaveType, cancellationToken);

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

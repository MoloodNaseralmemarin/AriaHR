using AriaHR.Modules.Requests.Application.DTOs;
using AriaHR.Modules.Requests.Application.Repositories;

namespace AriaHR.Modules.Requests.Application.UseCases.UpdateLeaveType;

public class UpdateLeaveTypeUseCase : IUpdateLeaveTypeUseCase
{
    private readonly ILeaveTypeRepository _leaveTypeRepository;

    public UpdateLeaveTypeUseCase(ILeaveTypeRepository leaveTypeRepository)
    {
        _leaveTypeRepository = leaveTypeRepository ?? throw new ArgumentNullException(nameof(leaveTypeRepository));
    }

    public async Task<LeaveTypeResponse> ExecuteAsync(
        Guid id,
        UpdateLeaveTypeRequest request,
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

        string name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("نام نوع مرخصی الزامی است.", nameof(request.Name));
        }

        if (request.MaxDaysPerYear < 0)
        {
            throw new ArgumentException("حداکثر روزهای مرخصی در سال نمی‌تواند منفی باشد.", nameof(request.MaxDaysPerYear));
        }

        var leaveType = await _leaveTypeRepository.GetByIdAndOrganizationIdAsync(id, organizationId, cancellationToken);
        if (leaveType == null)
        {
            throw new KeyNotFoundException("نوع مرخصی مورد نظر یافت نشد.");
        }

        bool exists = await _leaveTypeRepository.ExistsByNameAsync(organizationId, name, id, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("نوع مرخصی با این نام در سازمان قبلاً ثبت شده است.");
        }

        leaveType.Name = name;
        leaveType.MaxDaysPerYear = request.MaxDaysPerYear;
        leaveType.IsPaid = request.IsPaid;
        leaveType.RequiresAttachment = request.RequiresAttachment;
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

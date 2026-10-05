using AriaHR.Modules.Requests.Application.DTOs;
using AriaHR.Modules.Requests.Application.Repositories;
using AriaHR.Modules.Requests.Domain.Enums;

namespace AriaHR.Modules.Requests.Application.UseCases.RejectLeaveRequest;

public class RejectLeaveRequestUseCase : IRejectLeaveRequestUseCase
{
    private readonly ILeaveRequestRepository _repository;

    public RejectLeaveRequestUseCase(ILeaveRequestRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<LeaveRequestDto> ExecuteAsync(
        Guid id,
        RejectLeaveRequestRequest request,
        Guid userId,
        Guid userOrganizationId,
        bool isSystemAdmin,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("شناسه درخواست الزامی است.", nameof(id));
        }

        if (request == null || string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException("علت رد درخواست الزامی است.", nameof(request));
        }

        if (request.Reason.Trim().Length > 500)
        {
            throw new ArgumentException("علت رد درخواست نمی‌تواند بیش از 500 کاراکتر باشد.", nameof(request.Reason));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("شناسه کاربر الزامی است.", nameof(userId));
        }

        var leaveRequest = await _repository.GetByIdAsync(id, cancellationToken);
        if (leaveRequest == null)
        {
            throw new KeyNotFoundException("درخواست مورد نظر یافت نشد.");
        }

        if (!isSystemAdmin)
        {
            if (userOrganizationId == Guid.Empty || leaveRequest.OrganizationId != userOrganizationId)
            {
                throw new UnauthorizedAccessException("این درخواست متعلق به سازمان شما نمی‌باشد.");
            }
        }

        if (leaveRequest.Status != RequestStatus.Pending)
        {
            throw new InvalidOperationException("تنها درخواست‌های در انتظار قابل رد می‌باشند.");
        }

        var now = DateTime.UtcNow;
        leaveRequest.Status = RequestStatus.Rejected;
        leaveRequest.RejectedReason = request.Reason.Trim();
        leaveRequest.UpdatedAtUtc = now;
        leaveRequest.UpdatedByUserId = userId;

        await _repository.UpdateAsync(leaveRequest, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return new LeaveRequestDto
        {
            Id = leaveRequest.Id,
            EmployeeId = leaveRequest.EmployeeId,
            OrganizationId = leaveRequest.OrganizationId,
            LeaveType = leaveRequest.LeaveType,
            Date = leaveRequest.Date,
            StartTime = leaveRequest.StartTime,
            EndTime = leaveRequest.EndTime,
            Reason = leaveRequest.Reason,
            Status = leaveRequest.Status,
            RejectedReason = leaveRequest.RejectedReason,
            ApprovedAt = leaveRequest.ApprovedAt,
            ApprovedBy = leaveRequest.ApprovedBy,
            CreatedAtUtc = leaveRequest.CreatedAtUtc
        };
    }
}

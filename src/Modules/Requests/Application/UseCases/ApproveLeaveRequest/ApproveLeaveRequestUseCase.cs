using AriaHR.Modules.Requests.Application.DTOs;
using AriaHR.Modules.Requests.Application.Repositories;
using AriaHR.Modules.Requests.Domain.Enums;

namespace AriaHR.Modules.Requests.Application.UseCases.ApproveLeaveRequest;

public class ApproveLeaveRequestUseCase : IApproveLeaveRequestUseCase
{
    private readonly ILeaveRequestRepository _repository;

    public ApproveLeaveRequestUseCase(ILeaveRequestRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<LeaveRequestDto> ExecuteAsync(
        Guid id,
        Guid userId,
        Guid userOrganizationId,
        bool isSystemAdmin,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("شناسه درخواست الزامی است.", nameof(id));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("شناسه کاربر الزامی است.", nameof(userId));
        }

        var request = await _repository.GetByIdAsync(id, cancellationToken);
        if (request == null)
        {
            throw new KeyNotFoundException("درخواست مورد نظر یافت نشد.");
        }

        if (!isSystemAdmin)
        {
            if (userOrganizationId == Guid.Empty || request.OrganizationId != userOrganizationId)
            {
                throw new UnauthorizedAccessException("این درخواست متعلق به سازمان شما نمی‌باشد.");
            }
        }

        if (request.Status != RequestStatus.Pending)
        {
            throw new InvalidOperationException("تنها درخواست‌های در انتظار قابل تایید می‌باشند.");
        }

        var now = DateTime.UtcNow;
        request.Status = RequestStatus.Approved;
        request.ApprovedAt = now;
        request.ApprovedBy = userId;
        request.UpdatedAtUtc = now;
        request.UpdatedByUserId = userId;

        await _repository.UpdateAsync(request, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return new LeaveRequestDto
        {
            Id = request.Id,
            EmployeeId = request.EmployeeId,
            OrganizationId = request.OrganizationId,
            LeaveType = request.LeaveType,
            Date = request.Date,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Reason = request.Reason,
            Status = request.Status,
            RejectedReason = request.RejectedReason,
            ApprovedAt = request.ApprovedAt,
            ApprovedBy = request.ApprovedBy,
            CreatedAtUtc = request.CreatedAtUtc
        };
    }
}

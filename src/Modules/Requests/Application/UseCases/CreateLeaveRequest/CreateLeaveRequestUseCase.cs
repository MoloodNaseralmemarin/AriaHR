using AriaHR.Modules.Requests.Application.DTOs;
using AriaHR.Modules.Requests.Application.Repositories;
using AriaHR.Shared.Services;
using AriaHR.Modules.Requests.Domain.Entities;
using AriaHR.Modules.Requests.Domain.Enums;
using LeaveTypeEnum = AriaHR.Modules.Requests.Domain.Enums.LeaveType;

namespace AriaHR.Modules.Requests.Application.UseCases.CreateLeaveRequest;

public class CreateLeaveRequestUseCase : ICreateLeaveRequestUseCase
{
    private readonly ILeaveRequestRepository _repository;
    private readonly ILeaveCategoryRepository _leaveCategoryRepository;
    private readonly IEmployeeLookupService _employeeLookupService;

    public CreateLeaveRequestUseCase(
        ILeaveRequestRepository repository,
        ILeaveCategoryRepository leaveCategoryRepository,
        IEmployeeLookupService employeeLookupService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _leaveCategoryRepository = leaveCategoryRepository ?? throw new ArgumentNullException(nameof(leaveCategoryRepository));
        _employeeLookupService = employeeLookupService ?? throw new ArgumentNullException(nameof(employeeLookupService));
    }

    public async Task<LeaveRequestDto> ExecuteAsync(
        CreateLeaveRequestRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("شناسه کاربر الزامی است.", nameof(userId));
        }

        var employeeDetails = await _employeeLookupService.GetEmployeeDetailsByUserIdAsync(userId, cancellationToken);
        if (!employeeDetails.HasValue)
        {
            throw new InvalidOperationException("اطلاعات پرسنلی کاربر یافت نشد.");
        }

        var (employeeId, organizationId) = employeeDetails.Value;

        if (request.LeaveCategoryId == Guid.Empty)
        {
            throw new ArgumentException("شناسه دسته‌بندی مرخصی الزامی است.", nameof(request.LeaveCategoryId));
        }

        var category = await _leaveCategoryRepository.GetByIdAsync(request.LeaveCategoryId, cancellationToken);
        if (category == null || category.IsDeleted || category.OrganizationId != organizationId)
        {
            throw new ArgumentException("دسته‌بندی مرخصی انتخابی نامعتبر است یا به سازمان شما تعلق ندارد.", nameof(request.LeaveCategoryId));
        }

        if (!category.IsActive)
        {
            throw new ArgumentException("دسته‌بندی مرخصی انتخابی غیرفعال است.", nameof(request.LeaveCategoryId));
        }

        if (request.StartDate == default)
        {
            throw new ArgumentException("تاریخ شروع مرخصی الزامی است.", nameof(request.StartDate));
        }

        if (!string.IsNullOrWhiteSpace(request.Reason) && request.Reason.Trim().Length > 500)
        {
            throw new ArgumentException("علت مرخصی نمی‌تواند بیش از 500 کاراکتر باشد.", nameof(request.Reason));
        }

        DateOnly effectiveEndDate;
        TimeOnly? startTime = null;
        TimeOnly? endTime = null;

        if (request.LeaveType == LeaveTypeEnum.Hourly)
        {
            if (!request.EndDate.HasValue || request.EndDate.Value == default)
            {
                effectiveEndDate = request.StartDate;
            }
            else if (request.EndDate.Value != request.StartDate)
            {
                throw new ArgumentException("در مرخصی ساعتی، تاریخ شروع و پایان باید برابر باشد.", nameof(request.EndDate));
            }
            else
            {
                effectiveEndDate = request.EndDate.Value;
            }

            if (!request.StartTime.HasValue)
            {
                throw new ArgumentException("زمان شروع برای مرخصی ساعتی الزامی است.", nameof(request.StartTime));
            }

            if (!request.EndTime.HasValue)
            {
                throw new ArgumentException("زمان پایان برای مرخصی ساعتی الزامی است.", nameof(request.EndTime));
            }

            if (request.StartTime.Value >= request.EndTime.Value)
            {
                throw new ArgumentException("زمان پایان مرخصی باید بعد از زمان شروع باشد.", nameof(request.EndTime));
            }

            startTime = request.StartTime;
            endTime = request.EndTime;
        }
        else if (request.LeaveType == LeaveTypeEnum.Daily)
        {
            if (!request.EndDate.HasValue || request.EndDate.Value == default)
            {
                throw new ArgumentException("تاریخ پایان مرخصی الزامی است.", nameof(request.EndDate));
            }

            if (request.EndDate.Value < request.StartDate)
            {
                throw new ArgumentException("تاریخ پایان مرخصی نمی‌تواند قبل از تاریخ شروع باشد.", nameof(request.EndDate));
            }

            effectiveEndDate = request.EndDate.Value;
            startTime = null;
            endTime = null;
        }
        else
        {
            throw new ArgumentException("نوع مرخصی نامعتبر است.", nameof(request.LeaveType));
        }

        var now = DateTime.UtcNow;
        var leaveRequest = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            OrganizationId = organizationId,
            LeaveType = request.LeaveType,
            LeaveCategoryId = request.LeaveCategoryId,
            StartDate = request.StartDate,
            EndDate = effectiveEndDate,
            StartTime = startTime,
            EndTime = endTime,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
            Status = RequestStatus.Pending,
            CreatedAtUtc = now,
            CreatedByUserId = userId
        };

        await _repository.AddAsync(leaveRequest, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return new LeaveRequestDto
        {
            Id = leaveRequest.Id,
            EmployeeId = leaveRequest.EmployeeId,
            OrganizationId = leaveRequest.OrganizationId,
            LeaveType = leaveRequest.LeaveType,
            LeaveCategoryId = leaveRequest.LeaveCategoryId,
            StartDate = leaveRequest.StartDate,
            EndDate = leaveRequest.EndDate,
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

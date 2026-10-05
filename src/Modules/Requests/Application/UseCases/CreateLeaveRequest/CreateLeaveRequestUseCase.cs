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
    private readonly IEmployeeLookupService _employeeLookupService;

    public CreateLeaveRequestUseCase(
        ILeaveRequestRepository repository,
        IEmployeeLookupService employeeLookupService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
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

        if (request.Date == default)
        {
            throw new ArgumentException("تاریخ مرخصی الزامی است.", nameof(request.Date));
        }

        if (!string.IsNullOrWhiteSpace(request.Reason) && request.Reason.Length > 500)
        {
            throw new ArgumentException("علت مرخصی نمی‌تواند بیش از 500 کاراکتر باشد.", nameof(request.Reason));
        }

        TimeOnly? startTime = null;
        TimeOnly? endTime = null;

        if (request.LeaveType == LeaveTypeEnum.Hourly)
        {
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
            Date = request.Date,
            StartTime = startTime,
            EndTime = endTime,
            Reason = request.Reason?.Trim(),
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

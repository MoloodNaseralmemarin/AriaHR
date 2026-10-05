using AriaHR.Modules.Requests.Application.DTOs;
using AriaHR.Modules.Requests.Application.Repositories;
using AriaHR.Shared.Services;

namespace AriaHR.Modules.Requests.Application.UseCases.GetLeaveRequestById;

public class GetLeaveRequestByIdUseCase : IGetLeaveRequestByIdUseCase
{
    private readonly ILeaveRequestRepository _repository;
    private readonly IEmployeeLookupService _employeeLookupService;

    public GetLeaveRequestByIdUseCase(
        ILeaveRequestRepository repository,
        IEmployeeLookupService employeeLookupService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _employeeLookupService = employeeLookupService ?? throw new ArgumentNullException(nameof(employeeLookupService));
    }

    public async Task<LeaveRequestDto?> ExecuteAsync(
        Guid id,
        Guid userId,
        bool isSystemAdmin,
        bool isCenterManager,
        Guid? userOrganizationId,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("شناسه درخواست الزامی است.", nameof(id));
        }

        var request = await _repository.GetByIdAsync(id, cancellationToken);
        if (request == null)
        {
            return null;
        }

        if (isSystemAdmin)
        {
            // Allowed
        }
        else if (isCenterManager)
        {
            if (!userOrganizationId.HasValue || userOrganizationId.Value == Guid.Empty || request.OrganizationId != userOrganizationId.Value)
            {
                throw new UnauthorizedAccessException("دسترسی به این درخواست مجاز نمی‌باشد.");
            }
        }
        else
        {
            var employeeDetails = await _employeeLookupService.GetEmployeeDetailsByUserIdAsync(userId, cancellationToken);
            if (!employeeDetails.HasValue || employeeDetails.Value.EmployeeId != request.EmployeeId)
            {
                throw new UnauthorizedAccessException("دسترسی به این درخواست مجاز نمی‌باشد.");
            }
        }

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

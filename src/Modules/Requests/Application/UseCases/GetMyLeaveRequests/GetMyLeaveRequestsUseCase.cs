using AriaHR.Modules.Requests.Application.DTOs;
using AriaHR.Modules.Requests.Application.Repositories;
using AriaHR.Modules.Requests.Application.Services;

namespace AriaHR.Modules.Requests.Application.UseCases.GetMyLeaveRequests;

public class GetMyLeaveRequestsUseCase : IGetMyLeaveRequestsUseCase
{
    private readonly ILeaveRequestRepository _repository;
    private readonly IEmployeeLookupService _employeeLookupService;

    public GetMyLeaveRequestsUseCase(
        ILeaveRequestRepository repository,
        IEmployeeLookupService employeeLookupService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _employeeLookupService = employeeLookupService ?? throw new ArgumentNullException(nameof(employeeLookupService));
    }

    public async Task<IEnumerable<LeaveRequestDto>> ExecuteAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("شناسه کاربر الزامی است.", nameof(userId));
        }

        var employeeDetails = await _employeeLookupService.GetEmployeeDetailsByUserIdAsync(userId, cancellationToken);
        if (!employeeDetails.HasValue)
        {
            return Enumerable.Empty<LeaveRequestDto>();
        }

        var (employeeId, _) = employeeDetails.Value;

        var requests = await _repository.GetByEmployeeIdAsync(employeeId, cancellationToken);

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

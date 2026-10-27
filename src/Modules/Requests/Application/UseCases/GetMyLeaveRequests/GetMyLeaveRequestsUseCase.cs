using AriaHR.Modules.Requests.Application.DTOs;
using AriaHR.Modules.Requests.Application.Repositories;
using AriaHR.Shared.Services;

namespace AriaHR.Modules.Requests.Application.UseCases.GetMyLeaveRequests;

public class GetMyLeaveRequestsUseCase : IGetMyLeaveRequestsUseCase
{
    private readonly ILeaveRequestRepository _repository;
    private readonly ILeaveCategoryRepository _leaveCategoryRepository;
    private readonly IEmployeeLookupService _employeeLookupService;

    public GetMyLeaveRequestsUseCase(
        ILeaveRequestRepository repository,
        ILeaveCategoryRepository leaveCategoryRepository,
        IEmployeeLookupService employeeLookupService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _leaveCategoryRepository = leaveCategoryRepository ?? throw new ArgumentNullException(nameof(leaveCategoryRepository));
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

        if (!requests.Any())
        {
            return Enumerable.Empty<LeaveRequestDto>();
        }

        var employeeNames = await _employeeLookupService.GetEmployeeNamesByIdsAsync(new[] { employeeId }, cancellationToken);
        var categoryIds = requests.Select(r => r.LeaveCategoryId).Distinct();
        var categoryNames = await _leaveCategoryRepository.GetNamesByIdsAsync(categoryIds, cancellationToken);

        employeeNames.TryGetValue(employeeId, out var empName);

        return requests.Select(r =>
        {
            categoryNames.TryGetValue(r.LeaveCategoryId, out var categoryName);

            return new LeaveRequestDto
            {
                Id = r.Id,
                EmployeeId = r.EmployeeId,
                OrganizationId = r.OrganizationId,
                LeaveType = r.LeaveType,
                LeaveCategoryId = r.LeaveCategoryId,
                EmployeeFirstName = empName.FirstName,
                EmployeeLastName = empName.LastName,
                EmployeeFullName = empName.FullName,
                LeaveCategoryName = categoryName,
                StartDate = r.StartDate,
                EndDate = r.EndDate,
                StartTime = r.StartTime,
                EndTime = r.EndTime,
                Reason = r.Reason,
                Status = r.Status,
                RejectedReason = r.RejectedReason,
                ApprovedAt = r.ApprovedAt,
                ApprovedBy = r.ApprovedBy,
                CreatedAtUtc = r.CreatedAtUtc
            };
        });
    }
}

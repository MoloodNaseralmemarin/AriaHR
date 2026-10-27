using AriaHR.Modules.Requests.Application.DTOs;
using AriaHR.Modules.Requests.Application.Repositories;
using AriaHR.Shared.Services;

namespace AriaHR.Modules.Requests.Application.UseCases.GetOrganizationLeaveRequests;

public class GetOrganizationLeaveRequestsUseCase : IGetOrganizationLeaveRequestsUseCase
{
    private readonly ILeaveRequestRepository _repository;
    private readonly ILeaveCategoryRepository _leaveCategoryRepository;
    private readonly IEmployeeLookupService _employeeLookupService;

    public GetOrganizationLeaveRequestsUseCase(
        ILeaveRequestRepository repository,
        ILeaveCategoryRepository leaveCategoryRepository,
        IEmployeeLookupService employeeLookupService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _leaveCategoryRepository = leaveCategoryRepository ?? throw new ArgumentNullException(nameof(leaveCategoryRepository));
        _employeeLookupService = employeeLookupService ?? throw new ArgumentNullException(nameof(employeeLookupService));
    }

    public async Task<IEnumerable<LeaveRequestDto>> ExecuteAsync(
        Guid organizationId,
        GetOrganizationRequestsQuery? query,
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("شناسه سازمان الزامی است.", nameof(organizationId));
        }

        var requests = await _repository.GetOrganizationRequestsAsync(
            organizationId,
            query?.Status,
            query?.EmployeeId,
            query?.From,
            query?.To,
            cancellationToken);

        if (!requests.Any())
        {
            return Enumerable.Empty<LeaveRequestDto>();
        }

        var employeeIds = requests.Select(r => r.EmployeeId).Distinct();
        var categoryIds = requests.Select(r => r.LeaveCategoryId).Distinct();

        var employeeNames = await _employeeLookupService.GetEmployeeNamesByIdsAsync(employeeIds, cancellationToken);
        var categoryNames = await _leaveCategoryRepository.GetNamesByIdsAsync(categoryIds, cancellationToken);

        return requests.Select(r =>
        {
            employeeNames.TryGetValue(r.EmployeeId, out var empName);
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

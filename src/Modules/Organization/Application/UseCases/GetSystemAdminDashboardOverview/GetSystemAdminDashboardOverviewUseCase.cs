using AriaHR.Modules.Identity.Application.Repositories;
using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Repositories;

namespace AriaHR.Modules.Organization.Application.UseCases.GetSystemAdminDashboardOverview;

public class GetSystemAdminDashboardOverviewUseCase : IGetSystemAdminDashboardOverviewUseCase
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUserRepository _userRepository;

    public GetSystemAdminDashboardOverviewUseCase(
        IOrganizationRepository organizationRepository,
        IEmployeeRepository employeeRepository,
        IUserRepository userRepository)
    {
        _organizationRepository = organizationRepository ?? throw new ArgumentNullException(nameof(organizationRepository));
        _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<SystemAdminDashboardOverviewDto> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var (totalCenters, activeCenters, awaitingCompletionCenters) = await _organizationRepository.GetCentersOverviewAsync(cancellationToken);
        var centerManagerCount = await _userRepository.GetCountByRoleNameAsync("CenterManager", cancellationToken);
        var (totalEmployees, createdThisMonthEmployees) = await _employeeRepository.GetEmployeesSummaryAsync(null, cancellationToken);

        return new SystemAdminDashboardOverviewDto
        {
            Centers = new CentersSummaryDto
            {
                TotalCount = totalCenters,
                ActiveCount = activeCenters,
                AwaitingCompletionCount = awaitingCompletionCenters
            },
            CenterManagers = new CenterManagersSummaryDto
            {
                TotalCount = centerManagerCount
            },
            Employees = new EmployeesSummaryDto
            {
                TotalCount = totalEmployees,
                CreatedThisMonth = createdThisMonthEmployees
            }
        };
    }
}

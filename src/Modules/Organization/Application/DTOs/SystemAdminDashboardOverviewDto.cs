namespace AriaHR.Modules.Organization.Application.DTOs;

public class SystemAdminDashboardOverviewDto
{
    public CentersSummaryDto Centers { get; set; } = new();
    public CenterManagersSummaryDto CenterManagers { get; set; } = new();
    public EmployeesSummaryDto Employees { get; set; } = new();
}

public class CentersSummaryDto
{
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int AwaitingCompletionCount { get; set; }
}

public class CenterManagersSummaryDto
{
    public int TotalCount { get; set; }
}

public class EmployeesSummaryDto
{
    public int TotalCount { get; set; }
    public int CreatedThisMonth { get; set; }
}

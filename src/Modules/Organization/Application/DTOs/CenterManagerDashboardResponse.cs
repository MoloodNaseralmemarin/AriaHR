namespace AriaHR.Modules.Organization.Application.DTOs;

public class CenterManagerDashboardResponse
{
    public int ActiveEmployeesCount { get; set; }
    public int PresentTodayCount { get; set; }
    public int AbsentTodayCount { get; set; }
    public int ApprovedLeaveTodayCount { get; set; }
    public int LateArrivalsTodayCount { get; set; }
}

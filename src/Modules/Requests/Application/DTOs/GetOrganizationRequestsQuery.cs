using AriaHR.Modules.Requests.Domain.Enums;

namespace AriaHR.Modules.Requests.Application.DTOs;

public class GetOrganizationRequestsQuery
{
    public RequestStatus? Status { get; set; }
    public Guid? EmployeeId { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public Guid? OrganizationId { get; set; }
}

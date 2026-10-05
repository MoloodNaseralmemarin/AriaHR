namespace AriaHR.Shared.Services;

public interface IEmployeeLookupService
{
    Task<(Guid EmployeeId, Guid OrganizationId)?> GetEmployeeDetailsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}

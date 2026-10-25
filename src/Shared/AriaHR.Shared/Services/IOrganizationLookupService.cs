namespace AriaHR.Shared.Services;

public interface IOrganizationLookupService
{
    Task<string?> GetOrganizationNameByIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<string?> GetOrganizationNameForUserAsync(Guid? organizationId, string? phoneNumber, Guid userId, CancellationToken cancellationToken = default);
}

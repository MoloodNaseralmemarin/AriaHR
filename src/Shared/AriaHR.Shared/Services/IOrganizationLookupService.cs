namespace AriaHR.Shared.Services;

public interface IOrganizationLookupService
{
    Task<string?> GetOrganizationNameByIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
}

using AriaHR.Modules.Organization.Application.DTOs;

namespace AriaHR.Modules.Organization.Application.UseCases.GetOrganizationCenters;

public interface IGetOrganizationCentersUseCase
{
    Task<IEnumerable<BranchListItemDto>> ExecuteAsync(
        Guid organizationId,
        bool isSystemAdmin,
        Guid? userOrganizationId,
        CancellationToken cancellationToken = default);
}

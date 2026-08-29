using AriaHR.Modules.Organization.Application.DTOs;

namespace AriaHR.Modules.Organization.Application.UseCases.GetRecentOrganizations;

public interface IGetRecentOrganizationsUseCase
{
    Task<IReadOnlyList<RecentOrganizationDto>> ExecuteAsync(CancellationToken cancellationToken = default);
}

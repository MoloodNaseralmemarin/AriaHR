using AriaHR.Modules.Organization.Application.DTOs;

namespace AriaHR.Modules.Organization.Application.UseCases.GetRecentCenters;

public interface IGetRecentCentersUseCase
{
    Task<PagedResult<RecentCenterDto>> ExecuteAsync(int page = 1, int pageSize = 5, CancellationToken cancellationToken = default);
}

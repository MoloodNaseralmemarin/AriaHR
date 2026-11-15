using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Repositories;
using AriaHR.Modules.Organization.Application.UseCases.CreateWorkLocation;

namespace AriaHR.Modules.Organization.Application.UseCases.GetWorkLocations;

public class GetWorkLocationsUseCase : IGetWorkLocationsUseCase
{
    private readonly IWorkLocationRepository _workLocationRepository;

    public GetWorkLocationsUseCase(IWorkLocationRepository workLocationRepository)
    {
        _workLocationRepository = workLocationRepository ?? throw new ArgumentNullException(nameof(workLocationRepository));
    }

    public async Task<WorkLocationDto> GetByIdAsync(
        Guid workLocationId,
        Guid? targetOrganizationId = null,
        CancellationToken cancellationToken = default)
    {
        if (workLocationId == Guid.Empty)
        {
            throw new ArgumentException("شناسه محل کار الزامی است.");
        }

        var workLocation = await _workLocationRepository.GetByIdAsync(workLocationId, cancellationToken);
        if (workLocation == null)
        {
            throw new KeyNotFoundException("محل کار مورد نظر یافت نشد.");
        }

        if (targetOrganizationId.HasValue && targetOrganizationId.Value != Guid.Empty &&
            workLocation.OrganizationId != targetOrganizationId.Value)
        {
            throw new UnauthorizedAccessException("امکان مشاهده محل کار سازمان دیگر وجود ندارد.");
        }

        return WorkLocationDtoMapper.Map(workLocation);
    }

    public async Task<IReadOnlyList<WorkLocationDto>> GetByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("شناسه سازمان الزامی است.");
        }

        var locations = await _workLocationRepository.GetByOrganizationIdAsync(organizationId, cancellationToken);
        return locations.Select(WorkLocationDtoMapper.Map).ToList();
    }
}

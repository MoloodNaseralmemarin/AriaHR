using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Options;
using AriaHR.Modules.Organization.Application.Repositories;
using AriaHR.Modules.Organization.Application.UseCases.CreateWorkLocation;
using AriaHR.Modules.Organization.Application.Validators;
using Microsoft.Extensions.Options;

namespace AriaHR.Modules.Organization.Application.UseCases.UpdateWorkLocation;

public class UpdateWorkLocationUseCase : IUpdateWorkLocationUseCase
{
    private readonly IWorkLocationRepository _workLocationRepository;
    private readonly QrCodeOptions _options;

    public UpdateWorkLocationUseCase(
        IWorkLocationRepository workLocationRepository,
        IOptions<QrCodeOptions>? options = null)
    {
        _workLocationRepository = workLocationRepository ?? throw new ArgumentNullException(nameof(workLocationRepository));
        _options = options?.Value ?? new QrCodeOptions();
    }

    public async Task<WorkLocationDto> ExecuteAsync(
        Guid workLocationId,
        UpdateWorkLocationRequest request,
        Guid currentUserId,
        Guid? targetOrganizationId = null,
        CancellationToken cancellationToken = default)
    {
        if (workLocationId == Guid.Empty)
        {
            throw new ArgumentException("شناسه محل کار الزامی است.");
        }

        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        string name = WorkLocationInputValidator.ValidateName(request.Name);
        WorkLocationInputValidator.ValidateCoordinates(request.Latitude, request.Longitude);
        WorkLocationInputValidator.ValidateRadius(request.AllowedRadiusInMeters, _options);

        var workLocation = await _workLocationRepository.GetForUpdateAsync(workLocationId, cancellationToken);
        if (workLocation == null)
        {
            throw new KeyNotFoundException("محل کار مورد نظر یافت نشد.");
        }

        if (targetOrganizationId.HasValue && targetOrganizationId.Value != Guid.Empty &&
            workLocation.OrganizationId != targetOrganizationId.Value)
        {
            throw new UnauthorizedAccessException("امکان ویرایش محل کار سازمان دیگر وجود ندارد.");
        }

        workLocation.Name = name;
        workLocation.Latitude = request.Latitude;
        workLocation.Longitude = request.Longitude;
        workLocation.AllowedRadiusInMeters = request.AllowedRadiusInMeters;
        workLocation.IsActive = request.IsActive;

        if (!request.IsActive)
        {
            workLocation.QrCodeEnabled = false;
        }

        workLocation.UpdatedAtUtc = DateTime.UtcNow;
        workLocation.UpdatedByUserId = currentUserId == Guid.Empty ? null : currentUserId;

        _workLocationRepository.Update(workLocation);
        await _workLocationRepository.SaveChangesAsync(cancellationToken);

        return WorkLocationDtoMapper.Map(workLocation);
    }
}

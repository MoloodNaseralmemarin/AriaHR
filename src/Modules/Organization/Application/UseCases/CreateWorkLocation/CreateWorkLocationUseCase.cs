using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Repositories;
using AriaHR.Modules.Organization.Domain.Entities;

namespace AriaHR.Modules.Organization.Application.UseCases.CreateWorkLocation;

public class CreateWorkLocationUseCase : ICreateWorkLocationUseCase
{
    private readonly IWorkLocationRepository _workLocationRepository;

    public CreateWorkLocationUseCase(IWorkLocationRepository workLocationRepository)
    {
        _workLocationRepository = workLocationRepository ?? throw new ArgumentNullException(nameof(workLocationRepository));
    }

    public async Task<WorkLocationDto> ExecuteAsync(
        CreateWorkLocationRequest request,
        Guid organizationId,
        Guid createdByUserId,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("شناسه سازمان الزامی است.");
        }

        if (request.Latitude < -90 || request.Latitude > 90)
        {
            throw new ArgumentException("عرض جغرافیایی باید بین ۹۰- و ۹۰ باشد.");
        }

        if (request.Longitude < -180 || request.Longitude > 180)
        {
            throw new ArgumentException("طول جغرافیایی باید بین ۱۸۰- و ۱۸۰ باشد.");
        }

        if (request.RadiusInMeters <= 0)
        {
            throw new ArgumentException("شعاع محدوده باید بزرگتر از صفر باشد.");
        }

        var organizationExists = await _workLocationRepository.OrganizationExistsAsync(organizationId, cancellationToken);
        if (!organizationExists)
        {
            throw new ArgumentException("سازمان مورد نظر یافت نشد.");
        }

        var hasWorkLocation = await _workLocationRepository.HasWorkLocationAsync(organizationId, cancellationToken);
        if (hasWorkLocation)
        {
            throw new InvalidOperationException("برای این سازمان قبلاً محل کار ثبت شده است.");
        }

        var workLocation = new WorkLocation
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            RadiusInMeters = request.RadiusInMeters,
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        await _workLocationRepository.AddAsync(workLocation, cancellationToken);
        await _workLocationRepository.SaveChangesAsync(cancellationToken);

        return new WorkLocationDto
        {
            Id = workLocation.Id,
            OrganizationId = workLocation.OrganizationId,
            Latitude = workLocation.Latitude,
            Longitude = workLocation.Longitude,
            RadiusInMeters = workLocation.RadiusInMeters,
            IsActive = workLocation.IsActive,
            CreatedAtUtc = workLocation.CreatedAtUtc,
            CreatedByUserId = workLocation.CreatedByUserId
        };
    }
}

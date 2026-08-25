using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Repositories;

namespace AriaHR.Modules.Organization.Application.UseCases.CreateOrganization;

public class CreateOrganizationUseCase : ICreateOrganizationUseCase
{
    private readonly IOrganizationRepository _repository;

    public CreateOrganizationUseCase(IOrganizationRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<OrganizationDto> ExecuteAsync(
        CreateOrganizationRequest request,
        Guid createdByUserId,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Organization Name is required.", nameof(request.Name));
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new ArgumentException("Organization Code is required.", nameof(request.Code));
        }

        var organization = new Domain.Entities.Organization
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Code = request.Code.Trim(),
            NationalIdentifier = request.NationalIdentifier?.Trim(),
            Phone = request.Phone?.Trim(),
            Address = request.Address?.Trim(),
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        await _repository.AddAsync(organization, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return new OrganizationDto
        {
            Id = organization.Id,
            Name = organization.Name,
            Code = organization.Code,
            NationalIdentifier = organization.NationalIdentifier,
            Phone = organization.Phone,
            Address = organization.Address,
            IsActive = organization.IsActive,
            CreatedAtUtc = organization.CreatedAtUtc,
            CreatedByUserId = organization.CreatedByUserId
        };
    }
}

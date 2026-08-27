using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Repositories;
using AriaHR.Modules.Organization.Domain.Entities;

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

        if (!Enum.IsDefined(typeof(OrganizationType), request.Type))
        {
            throw new ArgumentException("Invalid Organization Type.", nameof(request.Type));
        }

        var organization = new Domain.Entities.Organization
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Code = request.Code.Trim(),
            Type = request.Type,
            NationalIdentifier = request.NationalIdentifier?.Trim(),
            Phone = request.Phone?.Trim(),
            Address = request.Address?.Trim(),
            ManagerFirstName = request.ManagerFirstName?.Trim(),
            ManagerLastName = request.ManagerLastName?.Trim(),
            ManagerMobile = request.ManagerMobile?.Trim(),
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
            Type = organization.Type,
            NationalIdentifier = organization.NationalIdentifier,
            Phone = organization.Phone,
            Address = organization.Address,
            ManagerFirstName = organization.ManagerFirstName,
            ManagerLastName = organization.ManagerLastName,
            ManagerMobile = organization.ManagerMobile,
            IsActive = organization.IsActive,
            CreatedAtUtc = organization.CreatedAtUtc,
            CreatedByUserId = organization.CreatedByUserId
        };
    }
}

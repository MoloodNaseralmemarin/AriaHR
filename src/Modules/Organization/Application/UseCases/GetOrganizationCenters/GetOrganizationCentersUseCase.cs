using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Repositories;

namespace AriaHR.Modules.Organization.Application.UseCases.GetOrganizationCenters;

public class GetOrganizationCentersUseCase : IGetOrganizationCentersUseCase
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IBranchRepository _branchRepository;

    public GetOrganizationCentersUseCase(
        IOrganizationRepository organizationRepository,
        IBranchRepository branchRepository)
    {
        _organizationRepository = organizationRepository ?? throw new ArgumentNullException(nameof(organizationRepository));
        _branchRepository = branchRepository ?? throw new ArgumentNullException(nameof(branchRepository));
    }

    public async Task<IEnumerable<BranchListItemDto>> ExecuteAsync(
        Guid organizationId,
        bool isSystemAdmin,
        Guid? userOrganizationId,
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("شناسه سازمان الزامی است.", nameof(organizationId));
        }

        if (!isSystemAdmin)
        {
            if (!userOrganizationId.HasValue || userOrganizationId.Value == Guid.Empty || userOrganizationId.Value != organizationId)
            {
                throw new UnauthorizedAccessException("شما دسترسی به مراکز این سازمان را ندارید.");
            }
        }

        var organization = await _organizationRepository.GetByIdAsync(organizationId, cancellationToken);
        if (organization == null)
        {
            throw new KeyNotFoundException("سازمان مورد نظر یافت نشد.");
        }

        var branches = await _branchRepository.GetByOrganizationIdAsync(organizationId, cancellationToken);

        return branches.Select(b => new BranchListItemDto
        {
            Id = b.Id,
            OrganizationId = b.OrganizationId,
            Name = b.Name,
            Code = b.Code,
            Address = b.Address,
            Phone = b.Phone,
            Email = b.Email,
            IsActive = b.IsActive
        });
    }
}

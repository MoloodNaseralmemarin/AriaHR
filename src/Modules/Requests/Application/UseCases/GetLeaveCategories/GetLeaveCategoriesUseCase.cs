using AriaHR.Modules.Requests.Application.DTOs;
using AriaHR.Modules.Requests.Application.Repositories;

namespace AriaHR.Modules.Requests.Application.UseCases.GetLeaveCategories;

public class GetLeaveCategoriesUseCase : IGetLeaveCategoriesUseCase
{
    private readonly ILeaveCategoryRepository _repository;

    public GetLeaveCategoriesUseCase(ILeaveCategoryRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<IEnumerable<LeaveCategoryDto>> ExecuteAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("شناسه سازمان الزامی است.", nameof(organizationId));
        }

        var categories = await _repository.GetByOrganizationAsync(organizationId, cancellationToken);

        return categories.Select(c => new LeaveCategoryDto
        {
            Id = c.Id,
            OrganizationId = c.OrganizationId,
            Name = c.Name,
            MaxDaysPerYear = c.MaxDaysPerYear,
            IsPaid = c.IsPaid,
            RequiresAttachment = c.RequiresAttachment,
            IsActive = c.IsActive
        });
    }
}

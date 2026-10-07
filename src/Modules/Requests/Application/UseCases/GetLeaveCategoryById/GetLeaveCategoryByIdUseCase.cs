using AriaHR.Modules.Requests.Application.DTOs;
using AriaHR.Modules.Requests.Application.Repositories;

namespace AriaHR.Modules.Requests.Application.UseCases.GetLeaveCategoryById;

public class GetLeaveCategoryByIdUseCase : IGetLeaveCategoryByIdUseCase
{
    private readonly ILeaveCategoryRepository _repository;

    public GetLeaveCategoryByIdUseCase(ILeaveCategoryRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<LeaveCategoryDto?> ExecuteAsync(
        Guid id,
        Guid userOrganizationId,
        bool isSystemAdmin,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("شناسه دسته‌بندی مرخصی الزامی است.", nameof(id));
        }

        var category = await _repository.GetByIdAsync(id, cancellationToken);
        if (category == null || category.IsDeleted)
        {
            return null;
        }

        if (!isSystemAdmin && category.OrganizationId != userOrganizationId)
        {
            throw new UnauthorizedAccessException("شما دسترسی به این دسته‌بندی مرخصی را ندارید.");
        }

        return new LeaveCategoryDto
        {
            Id = category.Id,
            OrganizationId = category.OrganizationId,
            Name = category.Name,
            MaxDaysPerYear = category.MaxDaysPerYear,
            IsPaid = category.IsPaid,
            RequiresAttachment = category.RequiresAttachment,
            IsActive = category.IsActive
        };
    }
}

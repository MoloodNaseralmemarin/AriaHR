using AriaHR.Modules.Requests.Application.DTOs;
using AriaHR.Modules.Requests.Application.Repositories;

namespace AriaHR.Modules.Requests.Application.UseCases.ActivateLeaveCategory;

public class ActivateLeaveCategoryUseCase : IActivateLeaveCategoryUseCase
{
    private readonly ILeaveCategoryRepository _repository;

    public ActivateLeaveCategoryUseCase(ILeaveCategoryRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<LeaveCategoryDto> ExecuteAsync(
        Guid id,
        Guid userId,
        Guid userOrganizationId,
        bool isSystemAdmin,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("شناسه دسته‌بندی مرخصی الزامی است.", nameof(id));
        }

        var category = await _repository.GetByIdAsync(id, cancellationToken);
        if (category == null)
        {
            throw new KeyNotFoundException("دسته‌بندی مرخصی مورد نظر یافت نشد.");
        }

        if (!isSystemAdmin && category.OrganizationId != userOrganizationId)
        {
            throw new UnauthorizedAccessException("شما دسترسی به تغییر وضعیت این دسته‌بندی مرخصی را ندارید.");
        }

        if (!category.IsActive)
        {
            category.IsActive = true;
            category.UpdatedAtUtc = DateTime.UtcNow;
            category.UpdatedByUserId = userId;

            await _repository.UpdateAsync(category, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
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

using AriaHR.Modules.Requests.Application.DTOs;
using AriaHR.Modules.Requests.Application.Repositories;

namespace AriaHR.Modules.Requests.Application.UseCases.UpdateLeaveCategory;

public class UpdateLeaveCategoryUseCase : IUpdateLeaveCategoryUseCase
{
    private readonly ILeaveCategoryRepository _repository;

    public UpdateLeaveCategoryUseCase(ILeaveCategoryRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<LeaveCategoryDto> ExecuteAsync(
        Guid id,
        UpdateLeaveCategoryRequest request,
        Guid userId,
        Guid userOrganizationId,
        bool isSystemAdmin,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

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
            throw new UnauthorizedAccessException("شما دسترسی به ویرایش این دسته‌بندی مرخصی را ندارید.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("نام دسته‌بندی مرخصی الزامی است.", nameof(request.Name));
        }

        var trimmedName = request.Name.Trim();
        if (trimmedName.Length > 200)
        {
            throw new ArgumentException("نام دسته‌بندی مرخصی نمی‌تواند بیش از 200 کاراکتر باشد.", nameof(request.Name));
        }

        var existingCategory = await _repository.GetByNameAndOrganizationAsync(trimmedName, category.OrganizationId, cancellationToken);
        if (existingCategory != null && existingCategory.Id != id)
        {
            throw new ArgumentException("دسته‌بندی مرخصی با این نام در سازمان وجود دارد.");
        }

        category.Name = trimmedName;
        category.MaxDaysPerYear = request.MaxDaysPerYear;
        category.IsPaid = request.IsPaid;
        category.RequiresAttachment = request.RequiresAttachment;
        category.UpdatedAtUtc = DateTime.UtcNow;
        category.UpdatedByUserId = userId;

        await _repository.UpdateAsync(category, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

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

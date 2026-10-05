using AriaHR.Modules.Requests.Application.DTOs;
using AriaHR.Modules.Requests.Application.Repositories;
using AriaHR.Modules.Requests.Domain.Entities;

namespace AriaHR.Modules.Requests.Application.UseCases.CreateLeaveCategory;

public class CreateLeaveCategoryUseCase : ICreateLeaveCategoryUseCase
{
    private readonly ILeaveCategoryRepository _repository;

    public CreateLeaveCategoryUseCase(ILeaveCategoryRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<LeaveCategoryDto> ExecuteAsync(
        CreateLeaveCategoryRequest request,
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("شناسه سازمان الزامی است.", nameof(organizationId));
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

        var existingCategory = await _repository.GetByNameAndOrganizationAsync(trimmedName, organizationId, cancellationToken);
        if (existingCategory != null)
        {
            throw new ArgumentException("دسته‌بندی مرخصی با این نام در سازمان وجود دارد.");
        }

        var now = DateTime.UtcNow;
        var category = new LeaveCategory
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = trimmedName,
            MaxDaysPerYear = request.MaxDaysPerYear,
            IsPaid = request.IsPaid,
            RequiresAttachment = request.RequiresAttachment,
            IsActive = true,
            CreatedAtUtc = now,
            CreatedByUserId = userId
        };

        await _repository.AddAsync(category, cancellationToken);
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

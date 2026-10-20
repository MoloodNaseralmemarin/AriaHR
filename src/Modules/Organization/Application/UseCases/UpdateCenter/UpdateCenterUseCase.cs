using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Repositories;

namespace AriaHR.Modules.Organization.Application.UseCases.UpdateCenter;

public class UpdateCenterUseCase : IUpdateCenterUseCase
{
    private readonly IBranchRepository _branchRepository;

    public UpdateCenterUseCase(IBranchRepository branchRepository)
    {
        _branchRepository = branchRepository ?? throw new ArgumentNullException(nameof(branchRepository));
    }

    public async Task<BranchListItemDto> ExecuteAsync(
        Guid centerId,
        UpdateBranchRequestDto request,
        Guid currentUserId,
        bool isSystemAdmin,
        Guid? userOrganizationId,
        CancellationToken cancellationToken = default)
    {
        if (centerId == Guid.Empty)
        {
            throw new ArgumentException("شناسه مرکز الزامی است.", nameof(centerId));
        }

        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("نام مرکز الزامی است.", nameof(request.Name));
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new ArgumentException("کد مرکز الزامی است.", nameof(request.Code));
        }

        string? normalizedEmail = null;
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var trimmedEmail = request.Email.Trim();
            var emailAttr = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
            int atIdx = trimmedEmail.IndexOf('@');
            if (!emailAttr.IsValid(trimmedEmail) || atIdx <= 0 || atIdx == trimmedEmail.Length - 1 || !trimmedEmail.Substring(atIdx + 1).Contains('.'))
            {
                throw new ArgumentException("فرمت ایمیل مرکز نامعتبر است.", nameof(request.Email));
            }
            normalizedEmail = trimmedEmail;
        }

        var branch = await _branchRepository.GetByIdAsync(centerId, cancellationToken);
        if (branch == null || branch.IsDeleted)
        {
            throw new KeyNotFoundException("مرکز مورد نظر یافت نشد.");
        }

        if (!isSystemAdmin)
        {
            if (!userOrganizationId.HasValue || userOrganizationId.Value == Guid.Empty || userOrganizationId.Value != branch.OrganizationId)
            {
                throw new UnauthorizedAccessException("شما دسترسی به ویرایش این مرکز را ندارید.");
            }
        }

        branch.Name = request.Name.Trim();
        branch.Code = request.Code.Trim();
        branch.Address = request.Address?.Trim();
        branch.Phone = request.Phone?.Trim();
        branch.Email = normalizedEmail;
        branch.UpdatedAtUtc = DateTime.UtcNow;
        branch.UpdatedByUserId = currentUserId;

        await _branchRepository.UpdateAsync(branch, cancellationToken);
        await _branchRepository.SaveChangesAsync(cancellationToken);

        return new BranchListItemDto
        {
            Id = branch.Id,
            OrganizationId = branch.OrganizationId,
            Name = branch.Name,
            Code = branch.Code,
            Address = branch.Address,
            Phone = branch.Phone,
            Email = branch.Email,
            IsActive = branch.IsActive
        };
    }
}

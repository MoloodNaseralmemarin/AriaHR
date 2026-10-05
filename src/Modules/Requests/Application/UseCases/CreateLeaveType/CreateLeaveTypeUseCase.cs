using AriaHR.Modules.Requests.Application.DTOs;
using AriaHR.Modules.Requests.Application.Repositories;
using AriaHR.Modules.Requests.Domain.Entities;

namespace AriaHR.Modules.Requests.Application.UseCases.CreateLeaveType;

public class CreateLeaveTypeUseCase : ICreateLeaveTypeUseCase
{
    private readonly ILeaveTypeRepository _leaveTypeRepository;

    public CreateLeaveTypeUseCase(ILeaveTypeRepository leaveTypeRepository)
    {
        _leaveTypeRepository = leaveTypeRepository ?? throw new ArgumentNullException(nameof(leaveTypeRepository));
    }

    public async Task<LeaveTypeResponse> ExecuteAsync(
        CreateLeaveTypeRequest request,
        Guid organizationId,
        Guid currentUserId,
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

        string name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("نام نوع مرخصی الزامی است.", nameof(request.Name));
        }

        if (request.MaxDaysPerYear < 0)
        {
            throw new ArgumentException("حداکثر روزهای مرخصی در سال نمی‌تواند منفی باشد.", nameof(request.MaxDaysPerYear));
        }

        bool exists = await _leaveTypeRepository.ExistsByNameAsync(organizationId, name, null, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("نوع مرخصی با این نام در سازمان قبلاً ثبت شده است.");
        }

        var now = DateTime.UtcNow;
        var leaveType = new LeaveType
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = name,
            MaxDaysPerYear = request.MaxDaysPerYear,
            IsPaid = request.IsPaid,
            RequiresAttachment = request.RequiresAttachment,
            IsActive = true,
            CreatedAtUtc = now,
            CreatedByUserId = currentUserId
        };

        await _leaveTypeRepository.AddAsync(leaveType, cancellationToken);

        return new LeaveTypeResponse
        {
            Id = leaveType.Id,
            Name = leaveType.Name,
            MaxDaysPerYear = leaveType.MaxDaysPerYear,
            IsPaid = leaveType.IsPaid,
            RequiresAttachment = leaveType.RequiresAttachment,
            IsActive = leaveType.IsActive
        };
    }
}

using AriaHR.Modules.Scheduling.Application.DTOs;
using AriaHR.Modules.Scheduling.Application.Repositories;
using AriaHR.Modules.Scheduling.Domain.Entities;

namespace AriaHR.Modules.Scheduling.Application.UseCases.DefineShift;

public class DefineShiftUseCase : IDefineShiftUseCase
{
    private readonly IShiftRepository _shiftRepository;

    public DefineShiftUseCase(IShiftRepository shiftRepository)
    {
        _shiftRepository = shiftRepository ?? throw new ArgumentNullException(nameof(shiftRepository));
    }

    public async Task<ShiftDto> ExecuteAsync(
        DefineShiftRequest request,
        Guid targetOrganizationId,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("نام شیفت الزامی است.", nameof(request));
        }

        if (targetOrganizationId == Guid.Empty)
        {
            throw new ArgumentException("شناسه سازمان الزامی است.", nameof(targetOrganizationId));
        }

        bool exists = await _shiftRepository.ExistsByNameAsync(request.Name.Trim(), targetOrganizationId, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("شیفتی با این نام در این سازمان قبلاً ثبت شده است.");
        }

        var shift = new Shift
        {
            Id = Guid.NewGuid(),
            OrganizationId = targetOrganizationId,
            Name = request.Name.Trim(),
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _shiftRepository.AddAsync(shift, cancellationToken);
        await _shiftRepository.SaveChangesAsync(cancellationToken);

        return new ShiftDto(
            shift.Id,
            shift.OrganizationId,
            shift.Name,
            shift.StartTime,
            shift.EndTime,
            shift.IsActive);
    }
}

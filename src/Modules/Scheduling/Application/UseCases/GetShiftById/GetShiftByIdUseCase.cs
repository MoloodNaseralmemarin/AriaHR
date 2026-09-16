using AriaHR.Modules.Scheduling.Application.DTOs;
using AriaHR.Modules.Scheduling.Application.Repositories;

namespace AriaHR.Modules.Scheduling.Application.UseCases.GetShiftById;

public class GetShiftByIdUseCase : IGetShiftByIdUseCase
{
    private readonly IShiftRepository _shiftRepository;

    public GetShiftByIdUseCase(IShiftRepository shiftRepository)
    {
        _shiftRepository = shiftRepository ?? throw new ArgumentNullException(nameof(shiftRepository));
    }

    public async Task<ShiftDto?> ExecuteAsync(
        Guid id,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        var shift = await _shiftRepository.GetByIdAsync(id, cancellationToken);
        if (shift == null || (organizationId != Guid.Empty && shift.OrganizationId != organizationId))
        {
            return null;
        }

        return new ShiftDto(
            shift.Id,
            shift.OrganizationId,
            shift.Name,
            shift.StartTime,
            shift.EndTime,
            shift.IsActive);
    }
}

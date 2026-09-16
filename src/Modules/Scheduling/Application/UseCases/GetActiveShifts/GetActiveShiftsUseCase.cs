using AriaHR.Modules.Scheduling.Application.DTOs;
using AriaHR.Modules.Scheduling.Application.Repositories;

namespace AriaHR.Modules.Scheduling.Application.UseCases.GetActiveShifts;

public class GetActiveShiftsUseCase : IGetActiveShiftsUseCase
{
    private readonly IShiftRepository _shiftRepository;

    public GetActiveShiftsUseCase(IShiftRepository shiftRepository)
    {
        _shiftRepository = shiftRepository ?? throw new ArgumentNullException(nameof(shiftRepository));
    }

    public async Task<IReadOnlyList<ShiftDto>> ExecuteAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("شناسه سازمان الزامی است.", nameof(organizationId));
        }

        var shifts = await _shiftRepository.GetActiveShiftsByOrganizationIdAsync(organizationId, cancellationToken);

        return shifts.Select(s => new ShiftDto(
            s.Id,
            s.OrganizationId,
            s.Name,
            s.StartTime,
            s.EndTime,
            s.IsActive
        )).ToList();
    }
}

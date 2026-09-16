using AriaHR.Modules.Scheduling.Application.DTOs;
using AriaHR.Modules.Scheduling.Application.Repositories;
using AriaHR.Modules.Scheduling.Domain.Entities;

namespace AriaHR.Modules.Scheduling.Application.UseCases.AssignShift;

public class AssignShiftUseCase : IAssignShiftUseCase
{
    private readonly IShiftAssignmentRepository _shiftAssignmentRepository;
    private readonly IShiftRepository _shiftRepository;

    public AssignShiftUseCase(
        IShiftAssignmentRepository shiftAssignmentRepository,
        IShiftRepository shiftRepository)
    {
        _shiftAssignmentRepository = shiftAssignmentRepository ?? throw new ArgumentNullException(nameof(shiftAssignmentRepository));
        _shiftRepository = shiftRepository ?? throw new ArgumentNullException(nameof(shiftRepository));
    }

    public async Task<ShiftAssignmentDto> ExecuteAsync(
        AssignShiftRequest request,
        Guid targetOrganizationId,
        Guid createdByUserId,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (targetOrganizationId == Guid.Empty)
        {
            throw new ArgumentException("شناسه سازمان الزامی است.", nameof(targetOrganizationId));
        }

        if (request.EmployeeId == Guid.Empty)
        {
            throw new ArgumentException("شناسه کارمند الزامی است.", nameof(request));
        }

        if (request.ShiftId == Guid.Empty)
        {
            throw new ArgumentException("شناسه شیفت الزامی است.", nameof(request));
        }

        var shift = await _shiftRepository.GetByIdAsync(request.ShiftId, cancellationToken);
        if (shift == null || shift.OrganizationId != targetOrganizationId)
        {
            throw new InvalidOperationException("شیفت مورد نظر در این سازمان یافت نشد.");
        }

        bool hasAssignment = await _shiftAssignmentRepository.HasAssignmentOnDateAsync(
            request.EmployeeId, request.Date, cancellationToken);

        if (hasAssignment)
        {
            throw new InvalidOperationException("برای این کارمند در این تاریخ قبلاً شیفت ثبت شده است.");
        }

        var assignment = new ShiftAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = targetOrganizationId,
            EmployeeId = request.EmployeeId,
            ShiftId = request.ShiftId,
            Date = request.Date,
            Status = "Assigned",
            CreatedBy = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _shiftAssignmentRepository.AddAsync(assignment, cancellationToken);
        await _shiftAssignmentRepository.SaveChangesAsync(cancellationToken);

        return new ShiftAssignmentDto(
            assignment.Id,
            assignment.OrganizationId,
            assignment.EmployeeId,
            assignment.ShiftId,
            shift.Name,
            shift.StartTime,
            shift.EndTime,
            assignment.Date,
            assignment.Status,
            assignment.CreatedBy);
    }
}

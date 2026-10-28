using AriaHR.Modules.Attendance.Domain.Entities;

namespace AriaHR.Modules.Attendance.Application.Repositories;

public interface IAttendanceRecordRepository
{
    Task<AttendanceRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AttendanceRecord?> GetActiveCheckInByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceRecord>> GetByEmployeeIdAsync(Guid employeeId, DateOnly? startDate = null, DateOnly? endDate = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceRecord>> GetByOrganizationIdAsync(Guid organizationId, DateOnly? startDate = null, DateOnly? endDate = null, Guid? employeeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(AttendanceRecord record, CancellationToken cancellationToken = default);
    Task UpdateAsync(AttendanceRecord record, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

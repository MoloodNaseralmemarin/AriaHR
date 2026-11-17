using AriaHR.Modules.Attendance.Application.DTOs;
using AriaHR.Modules.Attendance.Application.Repositories;
using AriaHR.Modules.Attendance.Domain.Entities;
using AriaHR.Modules.Attendance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Attendance.Infrastructure.Repositories;

public class AttendanceRecordRepository : IAttendanceRecordRepository
{
    private readonly AttendanceDbContext _dbContext;

    public AttendanceRecordRepository(AttendanceDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<AttendanceRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AttendanceRecords
            .FirstOrDefaultAsync(r => !r.IsDeleted && r.Id == id, cancellationToken);
    }

    public async Task<AttendanceRecord?> GetActiveCheckInByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AttendanceRecords
            .FirstOrDefaultAsync(r => !r.IsDeleted && r.EmployeeId == employeeId && r.CheckOutTime == null, cancellationToken);
    }

    public async Task<AttendanceRecord?> GetByEmployeeAndIdempotencyKeyAsync(
        Guid employeeId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty || string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return null;
        }

        return await _dbContext.AttendanceRecords
            .AsNoTracking()
            .Where(r => !r.IsDeleted && r.EmployeeId == employeeId &&
                        (r.CheckInIdempotencyKey == idempotencyKey || r.CheckOutIdempotencyKey == idempotencyKey))
            .OrderByDescending(r => r.CheckInTime)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<AttendanceRecord>> GetByEmployeeIdPagedAsync(
        Guid employeeId,
        int page,
        int pageSize,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            page = 1;
        }

        if (pageSize < 1 || pageSize > 100)
        {
            pageSize = 20;
        }

        var query = _dbContext.AttendanceRecords
            .AsNoTracking()
            .Where(r => !r.IsDeleted && r.EmployeeId == employeeId);

        if (startDate.HasValue)
        {
            var startUtc = startDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(r => r.CheckInTime >= startUtc);
        }

        if (endDate.HasValue)
        {
            var endUtc = endDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(r => r.CheckInTime < endUtc);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.CheckInTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AttendanceRecord>
        {
            Items = items,
            Total = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Page = page
        };
    }

    public async Task<int> GetByEmployeeIdCountAsync(
        Guid employeeId,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AttendanceRecords
            .AsNoTracking()
            .Where(r => !r.IsDeleted && r.EmployeeId == employeeId);

        if (startDate.HasValue)
        {
            var startUtc = startDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(r => r.CheckInTime >= startUtc);
        }

        if (endDate.HasValue)
        {
            var endUtc = endDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(r => r.CheckInTime < endUtc);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AttendanceRecord>> GetByOrganizationIdAsync(
        Guid organizationId,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        Guid? employeeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AttendanceRecords
            .AsNoTracking()
            .Where(r => !r.IsDeleted && r.OrganizationId == organizationId);

        if (employeeId.HasValue && employeeId.Value != Guid.Empty)
        {
            query = query.Where(r => r.EmployeeId == employeeId.Value);
        }

        if (startDate.HasValue)
        {
            var startUtc = startDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(r => r.CheckInTime >= startUtc);
        }

        if (endDate.HasValue)
        {
            var endUtc = endDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(r => r.CheckInTime < endUtc);
        }

        return await query
            .OrderByDescending(r => r.CheckInTime)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AttendanceRecord record, CancellationToken cancellationToken = default)
    {
        await _dbContext.AttendanceRecords.AddAsync(record, cancellationToken);
    }

    public Task UpdateAsync(AttendanceRecord record, CancellationToken cancellationToken = default)
    {
        _dbContext.AttendanceRecords.Update(record);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

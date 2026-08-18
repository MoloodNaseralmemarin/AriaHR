using AriaHR.Modules.Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Attendance.Infrastructure.Persistence;

public sealed class AttendanceDbContext : DbContext
{
    public AttendanceDbContext(
        DbContextOptions<AttendanceDbContext> options)
        : base(options)
    {
    }

    public DbSet<AttendanceRecord> AttendanceRecords =>
        Set<AttendanceRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AttendanceDbContext).Assembly);
    }
}

using AriaHR.Modules.Scheduling.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Scheduling.Infrastructure.Persistence;

public sealed class SchedulingDbContext : DbContext
{
    public SchedulingDbContext(
        DbContextOptions<SchedulingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<ShiftAssignment> ShiftAssignments => Set<ShiftAssignment>();
    public DbSet<ShiftSwapRequest> ShiftSwapRequests => Set<ShiftSwapRequest>();
    public DbSet<Holiday> Holidays => Set<Holiday>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SchedulingDbContext).Assembly);
    }
}

using AriaHR.Modules.Requests.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Requests.Infrastructure.Persistence;

public sealed class RequestsDbContext : DbContext
{
    public RequestsDbContext(
        DbContextOptions<RequestsDbContext> options)
        : base(options)
    {
    }

    public DbSet<LeaveType> LeaveTypes =>
        Set<LeaveType>();

    public DbSet<LeaveBalance> LeaveBalances =>
        Set<LeaveBalance>();

    public DbSet<LeaveRequest> LeaveRequests =>
        Set<LeaveRequest>();

    public DbSet<MissionRequest> MissionRequests =>
        Set<MissionRequest>();

    public DbSet<MissionLocationLog> MissionLocationLogs =>
        Set<MissionLocationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(RequestsDbContext).Assembly);
    }
}

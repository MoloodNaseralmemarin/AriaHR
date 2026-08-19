using AriaHR.Modules.Reporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Reporting.Infrastructure.Persistence;

public sealed class ReportingDbContext : DbContext
{
    public ReportingDbContext(
        DbContextOptions<ReportingDbContext> options)
        : base(options)
    {
    }

    public DbSet<AuditLog> AuditLogs =>
        Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ReportingDbContext).Assembly);
    }
}

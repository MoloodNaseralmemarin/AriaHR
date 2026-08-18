using AriaHR.Modules.Organization.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Organization.Infrastructure.Persistence;

public sealed class OrganizationDbContext : DbContext
{
    public OrganizationDbContext(
        DbContextOptions<OrganizationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<WorkLocation> WorkLocations => Set<WorkLocation>();
    public DbSet<QRCode> QRCodes => Set<QRCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(OrganizationDbContext).Assembly);
    }
}

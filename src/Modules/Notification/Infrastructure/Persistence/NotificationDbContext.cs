using AriaHR.Modules.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Notification.Infrastructure.Persistence;

public sealed class NotificationDbContext : DbContext
{
    public NotificationDbContext(
        DbContextOptions<NotificationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Domain.Entities.Notification> Notifications =>
        Set<Domain.Entities.Notification>();

    public DbSet<UserDevice> UserDevices =>
        Set<UserDevice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(NotificationDbContext).Assembly);
    }
}

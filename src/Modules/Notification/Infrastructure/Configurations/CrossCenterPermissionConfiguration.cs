using AriaHR.Modules.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AriaHR.Modules.Notification.Infrastructure.Configurations;

public class CrossCenterPermissionConfiguration : IEntityTypeConfiguration<CrossCenterPermission>
{
    public void Configure(EntityTypeBuilder<CrossCenterPermission> builder)
    {
        builder.ToTable("CrossCenterPermissions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.SourceOrganizationId)
            .IsRequired();

        builder.Property(p => p.DestinationOrganizationId)
            .IsRequired();

        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasIndex(p => new { p.SourceOrganizationId, p.DestinationOrganizationId });
    }
}

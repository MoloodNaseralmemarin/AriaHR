using AriaHR.Modules.Organization.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AriaHR.Modules.Organization.Infrastructure.Configurations;

public sealed class WorkLocationConfiguration : IEntityTypeConfiguration<WorkLocation>
{
    public void Configure(EntityTypeBuilder<WorkLocation> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.QrCodeReference)
            .HasMaxLength(64);

        builder.Property(x => x.QrCodeSecret)
            .HasMaxLength(128);

        builder.HasIndex(x => x.QrCodeReference)
            .IsUnique()
            .HasFilter("[QrCodeReference] IS NOT NULL");

        builder.HasIndex(x => x.OrganizationId);

        builder.HasMany(x => x.QRCodes)
            .WithOne(x => x.WorkLocation)
            .HasForeignKey(x => x.WorkLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

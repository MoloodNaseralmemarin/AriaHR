using AriaHR.Modules.Organization.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AriaHR.Modules.Organization.Infrastructure.Configurations;

public sealed class WorkLocationConfiguration : IEntityTypeConfiguration<WorkLocation>
{
    public void Configure(EntityTypeBuilder<WorkLocation> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Latitude)
            .HasPrecision(9, 6);

        builder.Property(x => x.Longitude)
            .HasPrecision(9, 6);

        builder.Property(x => x.AllowedRadiusInMeters)
            .HasPrecision(8, 1);

        builder.Property(x => x.QrCodeIdentifier)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.QrCodeMode)
            .HasConversion<int>();

        builder.HasIndex(x => x.OrganizationId);

        builder.HasIndex(x => x.QrCodeIdentifier)
            .IsUnique()
            .HasFilter("[QrCodeIdentifier] <> ''");

        builder.HasMany(x => x.QRCodes)
            .WithOne(x => x.WorkLocation)
            .HasForeignKey(x => x.WorkLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

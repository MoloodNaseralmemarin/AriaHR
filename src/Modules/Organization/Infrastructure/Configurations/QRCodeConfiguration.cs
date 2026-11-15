using AriaHR.Modules.Organization.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AriaHR.Modules.Organization.Infrastructure.Configurations;

public sealed class QRCodeConfiguration : IEntityTypeConfiguration<QRCode>
{
    public void Configure(EntityTypeBuilder<QRCode> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Title)
            .IsRequired();

        builder.HasIndex(x => x.Code);

        builder.HasIndex(x => new { x.WorkLocationId, x.IsActive });
    }
}

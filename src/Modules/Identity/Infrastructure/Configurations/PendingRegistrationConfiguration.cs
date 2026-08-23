using AriaHR.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AriaHR.Modules.Identity.Infrastructure.Configurations;

public sealed class PendingRegistrationConfiguration : IEntityTypeConfiguration<PendingRegistration>
{
    public void Configure(EntityTypeBuilder<PendingRegistration> builder)
    {
        builder.ToTable("PendingRegistrations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MobileNumber)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(x => x.MobileNumber);

        builder.Property(x => x.VerificationCodeHash)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.ExpiresAtUtc)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.AttemptCount)
            .IsRequired();

        builder.Property(x => x.IsVerified)
            .IsRequired();
    }
}

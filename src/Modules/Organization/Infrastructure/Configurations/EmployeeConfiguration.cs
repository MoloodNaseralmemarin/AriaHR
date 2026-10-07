using AriaHR.Modules.Organization.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AriaHR.Modules.Organization.Infrastructure.Configurations;

public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PersonnelCode)
            .IsRequired();

        builder.Property(x => x.NationalCode)
            .IsRequired();

        builder.Property(x => x.ProfileImage)
            .IsRequired(false);

        builder.Property(x => x.ProfileImageContentType)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.ProfileImageFileName)
            .HasMaxLength(255)
            .IsRequired(false);
    }
}

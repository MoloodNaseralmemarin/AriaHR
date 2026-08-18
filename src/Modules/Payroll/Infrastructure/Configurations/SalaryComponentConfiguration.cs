using AriaHR.Modules.Payroll.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AriaHR.Modules.Payroll.Infrastructure.Configurations;

public sealed class SalaryComponentConfiguration : IEntityTypeConfiguration<SalaryComponent>
{
    public void Configure(EntityTypeBuilder<SalaryComponent> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired();

        builder.Property(x => x.Code)
            .IsRequired();

        builder.Property(x => x.Type)
            .IsRequired();

        builder.Property(x => x.CalculationType)
            .IsRequired();

        builder.HasMany(x => x.SalaryStructureItems)
            .WithOne(x => x.SalaryComponent)
            .HasForeignKey(x => x.SalaryComponentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.PayrollItems)
            .WithOne(x => x.SalaryComponent)
            .HasForeignKey(x => x.SalaryComponentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

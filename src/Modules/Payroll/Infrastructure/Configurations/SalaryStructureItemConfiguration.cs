using AriaHR.Modules.Payroll.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AriaHR.Modules.Payroll.Infrastructure.Configurations;

public sealed class SalaryStructureItemConfiguration : IEntityTypeConfiguration<SalaryStructureItem>
{
    public void Configure(EntityTypeBuilder<SalaryStructureItem> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CalculationType)
            .IsRequired();
    }
}

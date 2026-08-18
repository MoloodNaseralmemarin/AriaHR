using AriaHR.Modules.Payroll.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AriaHR.Modules.Payroll.Infrastructure.Configurations;

public sealed class InsuranceRuleItemConfiguration : IEntityTypeConfiguration<InsuranceRuleItem>
{
    public void Configure(EntityTypeBuilder<InsuranceRuleItem> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ComponentType)
            .IsRequired();
    }
}

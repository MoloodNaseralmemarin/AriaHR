using AriaHR.Modules.Payroll.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AriaHR.Modules.Payroll.Infrastructure.Configurations;

public sealed class InsuranceRuleConfiguration : IEntityTypeConfiguration<InsuranceRule>
{
    public void Configure(EntityTypeBuilder<InsuranceRule> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired();

        builder.HasMany(x => x.InsuranceRuleItems)
            .WithOne(x => x.InsuranceRule)
            .HasForeignKey(x => x.InsuranceRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.InsuranceRecords)
            .WithOne(x => x.InsuranceRule)
            .HasForeignKey(x => x.InsuranceRuleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

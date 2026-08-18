using AriaHR.Modules.Payroll.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AriaHR.Modules.Payroll.Infrastructure.Configurations;

public sealed class TaxRuleConfiguration : IEntityTypeConfiguration<TaxRule>
{
    public void Configure(EntityTypeBuilder<TaxRule> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired();

        builder.HasMany(x => x.TaxBrackets)
            .WithOne(x => x.TaxRule)
            .HasForeignKey(x => x.TaxRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.TaxRecords)
            .WithOne(x => x.TaxRule)
            .HasForeignKey(x => x.TaxRuleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

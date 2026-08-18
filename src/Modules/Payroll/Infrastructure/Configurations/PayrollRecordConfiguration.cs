using AriaHR.Modules.Payroll.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AriaHR.Modules.Payroll.Infrastructure.Configurations;

public sealed class PayrollRecordConfiguration : IEntityTypeConfiguration<PayrollRecord>
{
    public void Configure(EntityTypeBuilder<PayrollRecord> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .IsRequired();

        builder.HasMany(x => x.PayrollItems)
            .WithOne(x => x.PayrollRecord)
            .HasForeignKey(x => x.PayrollRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.InsuranceRecord)
            .WithOne(x => x.PayrollRecord)
            .HasForeignKey<InsuranceRecord>(x => x.PayrollRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.TaxRecord)
            .WithOne(x => x.PayrollRecord)
            .HasForeignKey<TaxRecord>(x => x.PayrollRecordId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

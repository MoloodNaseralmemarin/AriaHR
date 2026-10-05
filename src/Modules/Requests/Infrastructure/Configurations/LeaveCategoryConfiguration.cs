using AriaHR.Modules.Requests.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AriaHR.Modules.Requests.Infrastructure.Configurations;

public sealed class LeaveCategoryConfiguration : IEntityTypeConfiguration<LeaveCategory>
{
    public void Configure(EntityTypeBuilder<LeaveCategory> builder)
    {
        builder.ToTable("LeaveCategories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired();

        builder.HasMany(x => x.LeaveBalances)
            .WithOne(x => x.LeaveCategory)
            .HasForeignKey(x => x.LeaveCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using AriaHR.Modules.Requests.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AriaHR.Modules.Requests.Infrastructure.Configurations;

public sealed class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("LeaveRequests");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrganizationId)
            .IsRequired();

        builder.Property(x => x.EmployeeId)
            .IsRequired();

        builder.Property(x => x.LeaveType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Date)
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasMaxLength(500);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.RejectedReason)
            .HasMaxLength(500);

        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => x.EmployeeId);
        builder.HasIndex(x => x.Status);
    }
}

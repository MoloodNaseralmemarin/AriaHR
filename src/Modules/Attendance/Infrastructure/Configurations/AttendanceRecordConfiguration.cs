using AriaHR.Modules.Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AriaHR.Modules.Attendance.Infrastructure.Configurations;

public sealed class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Method)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.DeviceInfo)
            .HasMaxLength(500);

        builder.Property(x => x.IPAddress)
            .HasMaxLength(100);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.Property(x => x.CheckInIdempotencyKey)
            .HasMaxLength(100);

        builder.Property(x => x.CheckOutIdempotencyKey)
            .HasMaxLength(100);

        builder.HasIndex(x => new { x.OrganizationId, x.EmployeeId, x.CheckInTime });
        builder.HasIndex(x => new { x.EmployeeId, x.CheckOutTime });

        builder.HasIndex(x => new { x.EmployeeId, x.CheckInIdempotencyKey })
            .IsUnique()
            .HasFilter("[CheckInIdempotencyKey] IS NOT NULL");

        builder.HasIndex(x => new { x.EmployeeId, x.CheckOutIdempotencyKey })
            .IsUnique()
            .HasFilter("[CheckOutIdempotencyKey] IS NOT NULL");
    }
}

using AriaHR.Modules.Requests.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AriaHR.Modules.Requests.Infrastructure.Configurations;

public sealed class MissionRequestConfiguration : IEntityTypeConfiguration<MissionRequest>
{
    public void Configure(EntityTypeBuilder<MissionRequest> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.HasMany(x => x.MissionLocationLogs)
            .WithOne(x => x.MissionRequest)
            .HasForeignKey(x => x.MissionRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

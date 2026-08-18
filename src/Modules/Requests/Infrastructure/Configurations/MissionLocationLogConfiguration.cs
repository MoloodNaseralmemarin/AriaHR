using AriaHR.Modules.Requests.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AriaHR.Modules.Requests.Infrastructure.Configurations;

public sealed class MissionLocationLogConfiguration : IEntityTypeConfiguration<MissionLocationLog>
{
    public void Configure(EntityTypeBuilder<MissionLocationLog> builder)
    {
        builder.HasKey(x => x.Id);
    }
}

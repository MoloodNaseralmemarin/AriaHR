using AriaHR.Modules.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AriaHR.Modules.Notification.Infrastructure.Configurations;

public class NotificationReplyConfiguration : IEntityTypeConfiguration<NotificationReply>
{
    public void Configure(EntityTypeBuilder<NotificationReply> builder)
    {
        builder.ToTable("NotificationReplies");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Content)
            .IsRequired();

        builder.HasQueryFilter(r => !r.IsDeleted);

        builder.HasIndex(r => new { r.NotificationId, r.RecipientUserId });
    }
}

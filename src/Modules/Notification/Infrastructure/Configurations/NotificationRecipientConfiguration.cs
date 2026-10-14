using AriaHR.Modules.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AriaHR.Modules.Notification.Infrastructure.Configurations;

public class NotificationRecipientConfiguration : IEntityTypeConfiguration<NotificationRecipient>
{
    public void Configure(EntityTypeBuilder<NotificationRecipient> builder)
    {
        builder.ToTable("NotificationRecipients");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.RecipientUserId)
            .IsRequired();

        builder.HasQueryFilter(r => !r.IsDeleted);

        builder.HasIndex(r => new { r.NotificationId, r.RecipientUserId });
        builder.HasIndex(r => r.RecipientUserId);
    }
}

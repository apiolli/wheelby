using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notifications.Domain.Inbox;

namespace Notifications.Infrastructure.Persistence;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();

        builder.Property(n => n.RecipientId).IsRequired();
        builder.Property(n => n.Type).HasMaxLength(Notification.TypeMaxLength).IsRequired();
        builder.Property(n => n.Title).HasMaxLength(Notification.TitleMaxLength).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(Notification.MessageMaxLength).IsRequired();
        builder.Property(n => n.ReferenceId);
        builder.Property(n => n.CreatedAt).IsRequired();
        builder.Property(n => n.ReadAt);
        // Sin clave foránea hacia QueuedEmails: el correo puede faltar (preferencia desactivada).
        builder.Property(n => n.EmailId);

        builder.Ignore(n => n.IsRead);

        // La bandeja se lee por destinatario, filtrada por leída y ordenada por fecha.
        builder.HasIndex(n => new { n.RecipientId, n.ReadAt, n.CreatedAt });
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notifications.Domain.Inbox;

namespace Notifications.Infrastructure.Persistence;

internal sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("NotificationPreferences");

        // Una fila por usuario, con el Id de AccessControl (sin clave foránea entre esquemas).
        builder.HasKey(p => p.UserId);
        builder.Property(p => p.UserId).ValueGeneratedNever();
        builder.Property(p => p.EmailEnabled).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();
    }
}

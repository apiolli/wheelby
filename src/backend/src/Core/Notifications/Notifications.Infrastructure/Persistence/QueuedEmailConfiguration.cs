using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notifications.Domain.Emails;

namespace Notifications.Infrastructure.Persistence;

internal sealed class QueuedEmailConfiguration : IEntityTypeConfiguration<QueuedEmail>
{
    public void Configure(EntityTypeBuilder<QueuedEmail> builder)
    {
        builder.ToTable("QueuedEmails");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.To)
            .HasConversion(v => v.Value, v => EmailAddress.Create(v))
            .HasMaxLength(EmailAddress.MaxLength)
            .IsRequired();

        builder.Property(e => e.Subject)
            .HasMaxLength(QueuedEmail.SubjectMaxLength)
            .IsRequired();

        builder.Property(e => e.Body).IsRequired();

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.SentAt);

        // RF-NOT-10: intentos y último error. La migración pone Attempts = 0 en las filas viejas.
        builder.Property(e => e.Attempts).IsRequired();
        builder.Property(e => e.LastAttemptAt);
        builder.Property(e => e.LastError).HasMaxLength(QueuedEmail.LastErrorMaxLength);
        builder.Property(e => e.FailedAt);

        builder.HasIndex(e => new { e.Status, e.CreatedAt });
    }
}
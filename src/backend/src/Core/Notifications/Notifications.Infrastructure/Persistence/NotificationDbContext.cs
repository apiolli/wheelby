using Microsoft.EntityFrameworkCore;
using Notifications.Domain.Emails;
using Notifications.Domain.Inbox;

namespace Notifications.Infrastructure.Persistence;

internal sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
    : DbContext(options)
{
    public const string Schema = "notifications";

    public DbSet<QueuedEmail> QueuedEmails => Set<QueuedEmail>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);
    }
}
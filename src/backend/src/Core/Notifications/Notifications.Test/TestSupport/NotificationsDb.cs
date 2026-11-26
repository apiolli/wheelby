using Microsoft.EntityFrameworkCore;
using Notifications.Infrastructure.Persistence;

namespace Notifications.Test.TestSupport;

// RD-12: NotificationsDbContext sobre EF InMemory, sin PostgreSQL.
// InMemory comparte los datos por nombre de base: cada prueba usa uno único.
internal static class NotificationsDb
{
    public static NotificationsDbContext CreaContexto()
        => new(new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseInMemoryDatabase($"notifications-tests-{Guid.NewGuid()}")
            .Options);
}

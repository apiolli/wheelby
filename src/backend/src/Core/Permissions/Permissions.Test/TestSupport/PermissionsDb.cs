using Microsoft.EntityFrameworkCore;
using Permissions.Infrastructure.Persistence;

namespace Permissions.Test.Support;

// RD-12: PermissionsDbContext sobre EF InMemory, sin PostgreSQL.
// InMemory comparte los datos por nombre de base: cada prueba usa uno único.
static class PermissionsDb
{
    public static PermissionsDbContext CreaContexto()
        => new(new DbContextOptionsBuilder<PermissionsDbContext>()
            .UseInMemoryDatabase($"permissions-tests-{Guid.NewGuid()}")
            .Options);
}

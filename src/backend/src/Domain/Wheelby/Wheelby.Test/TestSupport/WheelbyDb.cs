using Microsoft.EntityFrameworkCore;
using Wheelby.Infrastructure.Persistence;

namespace Wheelby.Test.TestSupport;

// RD-12: WheelbyDbContext sobre EF InMemory, sin PostgreSQL. Base con nombre único por prueba.
// La traducción real a SQL se verifica contra PostgreSQL (AGENTS.md §8).
internal static class WheelbyDb
{
    public static WheelbyDbContext CreaContexto()
        => new(new DbContextOptionsBuilder<WheelbyDbContext>()
            .UseInMemoryDatabase($"wheelby-tests-{Guid.NewGuid()}")
            .Options);
}

using Documents.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Documents.Test.TestSupport;

// DocumentsDbContext sobre EF InMemory, con un nombre de base único por prueba.
internal static class DocumentsDb
{
    public static DocumentsDbContext Create()
    {
        var options = new DbContextOptionsBuilder<DocumentsDbContext>()
            .UseInMemoryDatabase($"documents-{Guid.NewGuid():N}")
            .Options;
        return new DocumentsDbContext(options);
    }
}

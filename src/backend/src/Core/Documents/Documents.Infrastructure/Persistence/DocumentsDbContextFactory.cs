using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Documents.Infrastructure.Persistence;

internal sealed class DocumentsDbContextFactory : IDesignTimeDbContextFactory<DocumentsDbContext>
{
    public DocumentsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? throw new InvalidOperationException(
                "Falta la variable de entorno ConnectionStrings__Postgres.");

        var options = new DbContextOptionsBuilder<DocumentsDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", DocumentsDbContext.Schema))
            .Options;

        return new DocumentsDbContext(options);
    }
}

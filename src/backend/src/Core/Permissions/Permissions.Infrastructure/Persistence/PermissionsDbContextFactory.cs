using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Permissions.Infrastructure.Persistence;

internal sealed class PermissionsDbContextFactory : IDesignTimeDbContextFactory<PermissionsDbContext>
{
    public PermissionsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? throw new InvalidOperationException(
                "Falta la variable de entorno ConnectionStrings__Postgres.");

        var options = new DbContextOptionsBuilder<PermissionsDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", PermissionsDbContext.Schema))
            .Options;

        return new PermissionsDbContext(options);
    }
}

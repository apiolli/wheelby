using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Wheelby.Infrastructure.Persistence;

internal sealed class WheelbyDbContextFactory : IDesignTimeDbContextFactory<WheelbyDbContext>
{
    public WheelbyDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? throw new InvalidOperationException(
                "Falta la variable de entorno ConnectionStrings__Postgres.");

        var options = new DbContextOptionsBuilder<WheelbyDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", WheelbyDbContext.Schema))
            .Options;

        return new WheelbyDbContext(options);
    }
}

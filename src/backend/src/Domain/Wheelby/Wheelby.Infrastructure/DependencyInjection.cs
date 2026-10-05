using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wheelby.Infrastructure.Persistence;

namespace Wheelby.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddWheelby(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Falta la cadena de conexión ConnectionStrings__Postgres.");

        services.AddDbContext<WheelbyDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", WheelbyDbContext.Schema)));

        return services;
    }

    // Aplica la migración del esquema wheelby al arrancar.
    public static async Task ApplyWheelbyMigrationsAsync(
        this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WheelbyDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }
}

using AccessControl.Contracts;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Permissions.Contracts;
using Wheelby.Application.Abstractions;
using Wheelby.Application.Features.PublishVehicle;
using Wheelby.Infrastructure.Persistence;
using Wheelby.Infrastructure.Permissions;

namespace Wheelby.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddWheelby(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Falta la cadena de conexión ConnectionStrings__Postgres.");

        var applicationAssembly = typeof(PublishVehicleCommand).Assembly;
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly, includeInternalTypes: true);

        services.AddDbContext<WheelbyDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", WheelbyDbContext.Schema)));

        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IVehicleReadStore, VehicleReadStore>();
        services.AddScoped<IReservationReadStore, ReservationReadStore>();
        services.AddScoped<IWheelbyUnitOfWork, WheelbyUnitOfWork>();

        // Catálogo de permisos: cada módulo registra los suyos (inversión de dependencia).
        services.AddSingleton<IPermissionDefinition, PublishVehiclePermissionDefinition>();

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

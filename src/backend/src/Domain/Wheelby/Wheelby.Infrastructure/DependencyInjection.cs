using AccessControl.Contracts;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Permissions.Contracts;
using Wheelby.Application.Abstractions;
using Wheelby.Application.Features.PublishVehicle;
using Wheelby.Infrastructure.Persistence;
using Wheelby.Infrastructure.ExchangeRates;
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

        // RD-10: ExchangeRate-API. La clave llega solo por entorno (ExchangeRate__ApiKey).
        services.Configure<ExchangeRateOptions>(configuration.GetSection(ExchangeRateOptions.SectionName));
        services.AddMemoryCache();
        services.AddHttpClient<IExchangeRateProvider, ExchangeRateApiProvider>((sp, client) =>
        {
            var baseUrl = sp.GetRequiredService<IOptions<ExchangeRateOptions>>().Value.BaseUrl;
            client.BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/");
        });

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

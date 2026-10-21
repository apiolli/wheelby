using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Permissions.Application.Abstractions;
using Permissions.Application.Features.CreatePermissionRequest;
using Permissions.Contracts;
using Permissions.Infrastructure.Persistence;
using Permissions.Infrastructure.Permissions;

namespace Permissions.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPermissions(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Falta la cadena de conexión ConnectionStrings__Postgres.");

        var applicationAssembly = typeof(CreatePermissionRequestCommand).Assembly;
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly, includeInternalTypes: true);

        services.AddDbContext<PermissionsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", PermissionsDbContext.Schema)));

        services.AddScoped<IPermissionRequestRepository, PermissionRequestRepository>();
        services.AddScoped<IPermissionGrantRepository, PermissionGrantRepository>();
        services.AddScoped<IPermissionRequestReadStore, PermissionRequestReadStore>();
        services.AddScoped<IPermissionsUnitOfWork, PermissionsUnitOfWork>();

        // Catálogo de permisos: cada módulo registra los suyos (inversión de dependencia).
        services.AddSingleton<IPermissionDefinition, DemoPermissionDefinition>();
        services.AddSingleton<IPermissionCatalog, PermissionCatalog>();
        services.AddScoped<IPermissionChecker, PermissionChecker>();

        return services;
    }

    // Genera las migraciones al arrancar
    public static async Task ApplyPermissionsMigrationsAsync(
        this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PermissionsDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }
}

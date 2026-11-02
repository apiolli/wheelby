using AccessControl.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using Permissions.Application.Abstractions;
using Permissions.Application.Features.ApprovePermissionRequest;
using Permissions.Application.Features.CreatePermissionRequest;
using Permissions.Application.Features.RejectPermissionRequest;
using Permissions.Contracts;
using Permissions.Domain.PermissionRequests;
using Permissions.Infrastructure;
using Permissions.Infrastructure.Permissions;
using Permissions.Infrastructure.Persistence;
using Shared.Application;
using Shared.Application.Abstractions;
using SharedKernel.Exceptions;
using Wheelby.Application.Features.PublishVehicle;
using Wheelby.Infrastructure;
using Wheelby.Infrastructure.Persistence;
using Wheelby.Test.TestSupport;

// RF-NEG-06: prueba de integración con componentes reales (Permissions + Wheelby),
// única sustitución: bases de datos EF InMemory, ICurrentUser mutable y reloj fijo.
// No se usa IPermissionChecker falso ni repositorios falsos.
public class PermissionsIntegrationTests
{
    private sealed class InMemoryConfigurationProvider : ConfigurationProvider
    {
        public InMemoryConfigurationProvider(IDictionary<string, string?> data)
        {
            Data = data;
        }
        public override void Load() { }
    }

    private static IConfiguration CrearConfiguracion()
    {
        var provider = new InMemoryConfigurationProvider(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = "Host=localhost;Port=5432;Database=integration_test;Username=wheelby;Password=wheelby123"
        });
        provider.Load();
        return new ConfigurationRoot(new[] { provider });
    }

    private static void RemoverServicio<T>(IServiceCollection servicios)
    {
        var desc = servicios.FirstOrDefault(d => d.ServiceType == typeof(T));
        if (desc is not null) servicios.Remove(desc);
    }

    private static void ConfigurarIntegracion(IServiceCollection servicios, IConfiguration configuracion, Guid estandarId, Guid adminId)
    {
        servicios.AddLogging();
        servicios.AddSharedApplication();

        servicios.AddPermissions(configuracion);
        servicios.AddWheelby(configuracion);

        // Reemplazar IClock (SystemClock de AddSharedApplication) por el reloj fijo.
        RemoverServicio<IClock>(servicios);
        servicios.AddSingleton<IClock>(new FixedClock(new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc)));

        // Reconstruir IPermissionCatalog para incluir las definiciones de Wheelby (registradas por AddWheelby).
        RemoverServicio<IPermissionCatalog>(servicios);
        servicios.AddSingleton<IPermissionCatalog>(sp =>
        {
            var definiciones = sp.GetServices<IPermissionDefinition>().ToList();
            return new PermissionCatalog(definiciones);
        });

        var currentUser = new MutableCurrentUser(estandarId, "Standard");
        servicios.AddSingleton(typeof(ICurrentUser), currentUser);
    }

    [Fact]
    public async void RF_NEG_06_integracion_publicar_sin_aprobacion_es_rechazado_y_despues_de_aprobar_se_permite()
    {
        // Arrange: ServiceCollection real con AddPermissions + AddWheelby.
        var servicios = new ServiceCollection();

        var configuracion = CrearConfiguracion();
        var estandarId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        ConfigurarIntegracion(servicios, configuracion, estandarId, adminId);

        // Cambiar ambos DbContext a InMemory: eliminar DbContextOptions<T> y IDbContextOptionsConfiguration<T>.
        // RemoveAll no está disponible; eliminamos los descriptores manualmente.
        var opcionesPermisos = servicios.FirstOrDefault(d => d.ServiceType == typeof(DbContextOptions<PermissionsDbContext>));
        if (opcionesPermisos is not null) servicios.Remove(opcionesPermisos);
        foreach (var c in servicios.Where(d => d.ServiceType == typeof(IDbContextOptionsConfiguration<PermissionsDbContext>)).ToList())
            servicios.Remove(c);
        servicios.AddDbContext<PermissionsDbContext>(options =>
            options.UseInMemoryDatabase($"PermisosIntegration_{Guid.NewGuid():N}"));

        var opcionesWheelby = servicios.FirstOrDefault(d => d.ServiceType == typeof(DbContextOptions<WheelbyDbContext>));
        if (opcionesWheelby is not null) servicios.Remove(opcionesWheelby);
        foreach (var c in servicios.Where(d => d.ServiceType == typeof(IDbContextOptionsConfiguration<WheelbyDbContext>)).ToList())
            servicios.Remove(c);
        servicios.AddDbContext<WheelbyDbContext>(options =>
            options.UseInMemoryDatabase($"WheelbyIntegration_{Guid.NewGuid():N}"));

        var proveedor = servicios.BuildServiceProvider();
        var mediator = proveedor.GetRequiredService<IMediator>();
        var dbPermissions = proveedor.GetRequiredService<PermissionsDbContext>();
        var dbWheelby = proveedor.GetRequiredService<WheelbyDbContext>();
        var currentUser = (MutableCurrentUser)proveedor.GetRequiredService<ICurrentUser>();

        // --- Cara 1: el Estándar publica sin el permiso → 403, no hay vehículos.
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            mediator.Send(new PublishVehicleCommand("Toyota", "Corolla", 2020, "ABC-123", 15000m)));

        Assert.Empty(dbWheelby.Vehicles);

        // --- Paso 2: el Estándar crea la solicitud de wheelby.publish-vehicle → sigue sin permiso.
        var requestId = await mediator.Send(new CreatePermissionRequestCommand("wheelby.publish-vehicle"));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            mediator.Send(new PublishVehicleCommand("Toyota", "Corolla", 2020, "ABC-123", 15000m)));

        Assert.Empty(dbWheelby.Vehicles);

        // --- Paso 3: el Administrador aprueba la solicitud.
        currentUser.Id = adminId;
        currentUser.Role = "Administrator";
        await mediator.Send(new ApprovePermissionRequestCommand(requestId));

        // Verificar que la solicitud quedó Applied.
        var solicitud = dbPermissions.PermissionRequests.Single(r => r.Id == requestId);
        Assert.Equal(PermissionRequestStatus.Applied, solicitud.Status);

        // --- Paso 4: el Estándar publica → éxito, existe un vehículo del usuario.
        currentUser.Id = estandarId;
        currentUser.Role = "Standard";
        var idVehiculo = await mediator.Send(new PublishVehicleCommand("Toyota", "Corolla", 2020, "ABC-123", 15000m));

        Assert.NotEqual(Guid.Empty, idVehiculo);
        Assert.Single(dbWheelby.Vehicles);
        Assert.Equal(estandarId, dbWheelby.Vehicles.First().OwnerId);
    }

    [Fact]
    public async void RF_NEG_06_integracion_si_el_administrador_rechaza_publicar_sigue_rechazado()
    {
        // Arrange
        var servicios = new ServiceCollection();
        var configuracion = CrearConfiguracion();
        var estandarId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        ConfigurarIntegracion(servicios, configuracion, estandarId, adminId);

        // RemoveAll no está disponible; eliminamos los descriptores manualmente.
        var opcionesPermisos = servicios.FirstOrDefault(d => d.ServiceType == typeof(DbContextOptions<PermissionsDbContext>));
        if (opcionesPermisos is not null) servicios.Remove(opcionesPermisos);
        foreach (var c in servicios.Where(d => d.ServiceType == typeof(IDbContextOptionsConfiguration<PermissionsDbContext>)).ToList())
            servicios.Remove(c);
        servicios.AddDbContext<PermissionsDbContext>(options =>
            options.UseInMemoryDatabase($"PermisosRechazo_{Guid.NewGuid():N}"));

        var opcionesWheelby = servicios.FirstOrDefault(d => d.ServiceType == typeof(DbContextOptions<WheelbyDbContext>));
        if (opcionesWheelby is not null) servicios.Remove(opcionesWheelby);
        foreach (var c in servicios.Where(d => d.ServiceType == typeof(IDbContextOptionsConfiguration<WheelbyDbContext>)).ToList())
            servicios.Remove(c);
        servicios.AddDbContext<WheelbyDbContext>(options =>
            options.UseInMemoryDatabase($"WheelbyRechazo_{Guid.NewGuid():N}"));

        var proveedor = servicios.BuildServiceProvider();
        var mediator = proveedor.GetRequiredService<IMediator>();
        var dbPermissions = proveedor.GetRequiredService<PermissionsDbContext>();
        var dbWheelby = proveedor.GetRequiredService<WheelbyDbContext>();
        var currentUser = (MutableCurrentUser)proveedor.GetRequiredService<ICurrentUser>();

        // El Estándar publica sin permiso → 403.
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            mediator.Send(new PublishVehicleCommand("Toyota", "Corolla", 2020, "ABC-123", 15000m)));

        // Crea la solicitud.
        var requestId = await mediator.Send(new CreatePermissionRequestCommand("wheelby.publish-vehicle"));

        // El Administrador rechaza la solicitud.
        currentUser.Id = adminId;
        currentUser.Role = "Administrator";
        await mediator.Send(new RejectPermissionRequestCommand(requestId, "No cumple los requisitos"));

        // Verificar que la solicitud quedó Rejected.
        var solicitud = dbPermissions.PermissionRequests.Single(r => r.Id == requestId);
        Assert.Equal(PermissionRequestStatus.Rejected, solicitud.Status);

        // El Estándar intenta publicar de nuevo → sigue 403.
        currentUser.Id = estandarId;
        currentUser.Role = "Standard";
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            mediator.Send(new PublishVehicleCommand("Toyota", "Corolla", 2020, "ABC-123", 15000m)));

        Assert.Empty(dbWheelby.Vehicles);
    }

    [Fact]
    public async void RF_NEG_06_integracion_catalogo_solicitar_permiso_wheelby_es_aceptado()
    {
        // Arrange
        var servicios = new ServiceCollection();
        var configuracion = CrearConfiguracion();
        var estandarId = Guid.NewGuid();
        ConfigurarIntegracion(servicios, configuracion, estandarId, estandarId);

        // RemoveAll no está disponible; eliminamos los descriptores manualmente.
        var opcionesPermisos = servicios.FirstOrDefault(d => d.ServiceType == typeof(DbContextOptions<PermissionsDbContext>));
        if (opcionesPermisos is not null) servicios.Remove(opcionesPermisos);
        foreach (var c in servicios.Where(d => d.ServiceType == typeof(IDbContextOptionsConfiguration<PermissionsDbContext>)).ToList())
            servicios.Remove(c);
        servicios.AddDbContext<PermissionsDbContext>(options =>
            options.UseInMemoryDatabase($"PermisosCatalogo_{Guid.NewGuid():N}"));

        var opcionesWheelby = servicios.FirstOrDefault(d => d.ServiceType == typeof(DbContextOptions<WheelbyDbContext>));
        if (opcionesWheelby is not null) servicios.Remove(opcionesWheelby);
        foreach (var c in servicios.Where(d => d.ServiceType == typeof(IDbContextOptionsConfiguration<WheelbyDbContext>)).ToList())
            servicios.Remove(c);
        servicios.AddDbContext<WheelbyDbContext>(options =>
            options.UseInMemoryDatabase($"WheelbyCatalogo_{Guid.NewGuid():N}"));

        var proveedor = servicios.BuildServiceProvider();
        var mediator = proveedor.GetRequiredService<IMediator>();

        // Act: solicitar wheelby.publish-vehicle es aceptado (Wheelby registró su permiso en el catálogo).
        var requestId = await mediator.Send(new CreatePermissionRequestCommand("wheelby.publish-vehicle"));

        // Assert
        Assert.NotEqual(Guid.Empty, requestId);
    }
}

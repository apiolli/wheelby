using Shared.Application.Abstractions;
using SharedKernel.Exceptions;
using Wheelby.Application.Features.PublishVehicle;
using Wheelby.Application.Abstractions;
using Wheelby.Domain.Vehicles;
using Wheelby.Test.TestSupport;

// F1: Publicar vehículo. Pruebas unitarias con dobles hechos a mano.
// El FakePermissionChecker SOLO se usa aquí (nunca en la prueba de integración).
public class PublishVehicleHandlerTests
{
    private static readonly DateTime Ahora = new(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);

    private static PublishVehicleCommandHandler CrearHandler(
        out InMemoryVehicleRepository vehicles,
        out WheelbyRecordingUnitOfWork unitOfWork,
        out MutableCurrentUser currentUser,
        out FakePermissionChecker permissionChecker,
        bool conPermiso = false)
    {
        vehicles = new InMemoryVehicleRepository();
        unitOfWork = new WheelbyRecordingUnitOfWork();
        currentUser = new MutableCurrentUser(Guid.NewGuid(), "Standard");
        permissionChecker = new FakePermissionChecker();
        var clock = new FixedClock(Ahora);

        if (conPermiso)
            permissionChecker.Grant(currentUser.Id, "wheelby.publish-vehicle");

        return new PublishVehicleCommandHandler(
            vehicles, unitOfWork, permissionChecker, currentUser, clock);
    }

    [Fact]
    public void RF_NEG_02_F1_con_permiso_crea_vehiculo_con_owner_id_del_usuario_actual()
    {
        // Arrange
        var handler = CrearHandler(
            out var vehicles, out var unitOfWork, out var currentUser, out _, conPermiso: true);
        var command = new PublishVehicleCommand("Toyota", "Corolla", 2020, "ABC-123", 15000m);

        // Act
        var id = handler.Handle(command, CancellationToken.None).Result;

        // Assert
        Assert.NotEqual(Guid.Empty, id);
        var vehiculo = vehicles.Vehicles.FirstOrDefault();
        Assert.NotNull(vehiculo);
        Assert.Equal(currentUser.Id, vehiculo.OwnerId);
        Assert.Equal("ABC-123", vehiculo.Plate);
        Assert.Single(vehicles.Vehicles);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task RF_NEG_02_F1_sin_permiso_lanza_403_y_no_guarda_nada()
    {
        // Arrange
        var handler = CrearHandler(
            out var vehicles, out var unitOfWork, out var currentUser, out _, conPermiso: false);
        var command = new PublishVehicleCommand("Toyota", "Corolla", 2020, "ABC-123", 15000m);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(async () =>
            await handler.Handle(command, CancellationToken.None));

        Assert.Empty(vehicles.Vehicles);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task RF_NEG_02_F1_placa_duplicada_lanza_409()
    {
        // Arrange
        var handler = CrearHandler(
            out var vehicles, out var unitOfWork, out var currentUser, out _, conPermiso: true);
        var existing = Vehicle.Create(
            currentUser.Id, "Toyota", "Corolla", 2020, "ABC-123", 15000m, Ahora);
        vehicles.AddAsync(existing).GetAwaiter().GetResult();

        var command = new PublishVehicleCommand("Honda", "Civic", 2021, "ABC-123", 20000m);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(async () =>
            await handler.Handle(command, CancellationToken.None));

        Assert.Single(vehicles.Vehicles); // el existente sigue siendo el único
    }
}

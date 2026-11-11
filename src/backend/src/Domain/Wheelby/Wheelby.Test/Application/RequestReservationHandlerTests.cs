using AccessControl.Contracts;
using Shared.Application.Abstractions;
using SharedKernel.Exceptions;
using Wheelby.Application.Features.RequestReservation;
using Wheelby.Application.Abstractions;
using Wheelby.Domain.Reservations;
using Wheelby.Domain.Vehicles;
using Wheelby.Test.TestSupport;

// F3: Solicitar reserva.
public class RequestReservationHandlerTests
{
    private static readonly DateTime Ahora = new(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Inicio = new(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Fin = new(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc);

    private static RequestReservationCommandHandler CrearHandler(
        out InMemoryVehicleRepository vehicles,
        out InMemoryReservationRepository reservations,
        out WheelbyRecordingUnitOfWork unitOfWork,
        out MutableCurrentUser currentUser)
    {
        vehicles = new InMemoryVehicleRepository();
        reservations = new InMemoryReservationRepository();
        unitOfWork = new WheelbyRecordingUnitOfWork();
        currentUser = new MutableCurrentUser(Guid.NewGuid(), "Standard");
        var clock = new FixedClock(Ahora);
        return new RequestReservationCommandHandler(
            vehicles, reservations, unitOfWork, currentUser, clock);
    }

    [Fact]
    public async Task RF_NEG_02_F3_propio_vehiculo_lanza_403()
    {
        // Arrange
        var handler = CrearHandler(out var vehicles, out _, out _, out var currentUser);
        var vehiculo = Vehicle.Create(currentUser.Id, "Toyota", "Corolla", 2020, "ABC-123", 15000m, Ahora);
        await vehicles.AddAsync(vehiculo);

        var command = new RequestReservationCommand(vehiculo.Id, Inicio, Fin);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(async () =>
            await handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task RF_NEG_02_F3_vehiculo_inexistente_lanza_404()
    {
        // Arrange
        var handler = CrearHandler(out var vehicles, out _, out _, out _);
        var command = new RequestReservationCommand(Guid.NewGuid(), Inicio, Fin);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(async () =>
            await handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task RF_NEG_02_F3_solap_con_reserva_bloqueante_lanza_409()
    {
        // Arrange
        var handler = CrearHandler(out var vehicles, out var reservations, out _, out var currentUser);
        var propietario = Guid.NewGuid();
        var vehiculo = Vehicle.Create(propietario, "Toyota", "Corolla", 2020, "ABC-123", 15000m, Ahora);
        await vehicles.AddAsync(vehiculo);

        // Reserva Confirmed existente que solapa
        var existente = Reservation.Create(vehiculo.Id, Guid.NewGuid(), Inicio, Fin, Ahora);
        existente.TransitionTo(ReservationStatus.Confirmed, ReservationActor.Host, Ahora, null);
        await reservations.AddAsync(existente);

        var command = new RequestReservationCommand(vehiculo.Id, Inicio, Fin);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(async () =>
            await handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task RF_NEG_02_F3_exito_nace_en_Pending()
    {
        // Arrange
        var handler = CrearHandler(out var vehicles, out var reservations, out var unitOfWork, out var currentUser);
        var propietario = Guid.NewGuid();
        var vehiculo = Vehicle.Create(propietario, "Toyota", "Corolla", 2020, "ABC-123", 15000m, Ahora);
        await vehicles.AddAsync(vehiculo);

        var command = new RequestReservationCommand(vehiculo.Id, Inicio, Fin);

        // Act
        var id = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, id);
        var reserva = await reservations.GetByIdAsync(id);
        Assert.NotNull(reserva);
        Assert.Equal(ReservationStatus.Pending, reserva.Status);
        Assert.Equal(currentUser.Id, reserva.RenterId);
        Assert.Equal(1, unitOfWork.SaveCount);
    }
}

using Shared.Application.Abstractions;
using SharedKernel.Exceptions;
using Wheelby.Application.Features.ConfirmReservation;
using Wheelby.Application.Abstractions;
using Wheelby.Domain.Reservations;
using Wheelby.Domain.Reservations.Exceptions;
using Wheelby.Domain.Vehicles;
using Wheelby.Test.TestSupport;

// F4: Responder (confirmar / rechazar) reserva.
public class ConfirmReservationHandlerTests
{
    private static readonly DateTime Ahora = new(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Inicio = new(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Fin = new(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc);

    private static (ConfirmReservationCommandHandler handler, InMemoryVehicleRepository vehicles, InMemoryReservationRepository reservations, WheelbyRecordingUnitOfWork unitOfWork, MutableCurrentUser currentUser) CrearHandler(Guid? duenioVehiculo = null)
    {
        var vehicles = new InMemoryVehicleRepository();
        var reservations = new InMemoryReservationRepository();
        var unitOfWork = new WheelbyRecordingUnitOfWork();
        var currentUser = new MutableCurrentUser(Guid.NewGuid(), "Standard");
        var dueno = duenioVehiculo ?? currentUser.Id;
        var clock = new FixedClock(Ahora);

        var vehiculo = Vehicle.Create(dueno, "Toyota", "Corolla", 2020, "ABC-123", 15000m, Ahora);
        vehicles.AddAsync(vehiculo).GetAwaiter().GetResult();

        var reserva = Reservation.Create(vehiculo.Id, Guid.NewGuid(), Inicio, Fin, Ahora);
        reservations.AddAsync(reserva).GetAwaiter().GetResult();

        var handler = new ConfirmReservationCommandHandler(vehicles, reservations, unitOfWork, currentUser, clock);
        return (handler, vehicles, reservations, unitOfWork, currentUser);
    }

    [Fact]
    public async Task RF_NEG_02_F4_dueno_confirma_pasa_a_Confirmed()
    {
        // Arrange
        var (handler, vehicles, reservations, unitOfWork, currentUser) = CrearHandler();
        var reserva = reservations.Reservations.First();

        // Act
        await handler.Handle(new ConfirmReservationCommand(reserva.Id), CancellationToken.None);

        // Assert
        Assert.Equal(ReservationStatus.Confirmed, reserva.Status);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task RF_NEG_02_F4_no_dueno_confirma_lanza_403_y_estado_no_cambia()
    {
        // Arrange
        var otroUsuario = Guid.NewGuid();
        var (handler, _, reservations, unitOfWork, _) = CrearHandler(duenioVehiculo: otroUsuario);
        var reserva = reservations.Reservations.First();

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(async () =>
            await handler.Handle(new ConfirmReservationCommand(reserva.Id), CancellationToken.None));

        Assert.Equal(ReservationStatus.Pending, reserva.Status);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task RF_NEG_02_F4_confirmar_con_otra_reserva_bloqueante_solapada_lanza_409()
    {
        // Arrange
        var (handler, vehicles, reservations, unitOfWork, currentUser) = CrearHandler();
        var reserva = reservations.Reservations.First();

        // Otra reserva Confirmed que solapa las mismas fechas del mismo vehículo
        var otra = Reservation.Create(reserva.VehicleId, Guid.NewGuid(), Inicio, Fin, Ahora);
        otra.TransitionTo(ReservationStatus.Confirmed, ReservationActor.Host, Ahora, null);
        await reservations.AddAsync(otra);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(async () =>
            await handler.Handle(new ConfirmReservationCommand(reserva.Id), CancellationToken.None));

        Assert.Equal(ReservationStatus.Pending, reserva.Status);
    }

    [Fact]
    public async Task RF_NEG_02_F4_rechazar_sin_motivo_se_rechaza()
    {
        // Arrange
        var vehicles = new InMemoryVehicleRepository();
        var reservations = new InMemoryReservationRepository();
        var unitOfWork = new WheelbyRecordingUnitOfWork();
        var currentUser = new MutableCurrentUser(Guid.NewGuid(), "Standard");
        var clock = new FixedClock(Ahora);

        var vehiculo = Vehicle.Create(currentUser.Id, "Toyota", "Corolla", 2020, "ABC-123", 15000m, Ahora);
        await vehicles.AddAsync(vehiculo);

        var reserva = Reservation.Create(vehiculo.Id, Guid.NewGuid(), Inicio, Fin, Ahora);
        await reservations.AddAsync(reserva);

        var rejectHandler = new Wheelby.Application.Features.RejectReservation.RejectReservationCommandHandler(
            vehicles, reservations, unitOfWork, currentUser, clock);

        // Act & Assert: Host rechaza sin motivo
        await Assert.ThrowsAsync<ReservationTransitionNotAllowedException>(async () =>
            await rejectHandler.Handle(
                new Wheelby.Application.Features.RejectReservation.RejectReservationCommand(reserva.Id, null),
                CancellationToken.None));

        Assert.Equal(ReservationStatus.Pending, reserva.Status);
    }
}

using Shared.Application.Abstractions;
using SharedKernel.Exceptions;
using Wheelby.Application.Features.CancelReservation;
using Wheelby.Application.Features.CompleteRental;
using Wheelby.Application.Features.StartRental;
using Wheelby.Application.Abstractions;
using Wheelby.Domain.Reservations;
using Wheelby.Domain.Reservations.Exceptions;
using Wheelby.Domain.Vehicles;
using Wheelby.Test.TestSupport;

// F5: Ciclo de la reserva (iniciar, completar, cancelar).
public class RentalCycleHandlerTests
{
    private static readonly DateTime Ahora = new(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Inicio = new(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Fin = new(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc);

    private static (Guid reservaId, InMemoryVehicleRepository vehicles, InMemoryReservationRepository reservations, WheelbyRecordingUnitOfWork unitOfWork, MutableCurrentUser dueno, MutableCurrentUser inquilino) ConfigurarReservaConfirmada()
    {
        var vehicles = new InMemoryVehicleRepository();
        var reservations = new InMemoryReservationRepository();
        var unitOfWork = new WheelbyRecordingUnitOfWork();
        var dueno = new MutableCurrentUser(Guid.NewGuid(), "Standard");
        var inquilino = new MutableCurrentUser(Guid.NewGuid(), "Standard");
        var clock = new FixedClock(Ahora);

        var vehiculo = Vehicle.Create(dueno.Id, "Toyota", "Corolla", 2020, "ABC-123", 15000m, Ahora);
        vehicles.AddAsync(vehiculo).GetAwaiter().GetResult();

        var reserva = Reservation.Create(vehiculo.Id, inquilino.Id, Inicio, Fin, Ahora);
        reserva.TransitionTo(ReservationStatus.Confirmed, ReservationActor.Host, Ahora, null);
        reservations.AddAsync(reserva).GetAwaiter().GetResult();

        return (reserva.Id, vehicles, reservations, unitOfWork, dueno, inquilino);
    }

    [Fact]
    public async Task RF_NEG_02_F5_dueno_inicia_y_completa_el_alquiler()
    {
        // Arrange
        var (reservaId, vehicles, reservations, unitOfWork, dueno, _) = ConfigurarReservaConfirmada();

        // Act: iniciar (usar Inicio para cumplir la guarda StartDate <= utcNow)
        var startHandler = new StartRentalCommandHandler(vehicles, reservations, unitOfWork, dueno, new FixedClock(Inicio));
        await startHandler.Handle(new StartRentalCommand(reservaId), CancellationToken.None);

        // Assert: estado InProgress
        var reserva = reservations.Reservations.First();
        Assert.Equal(ReservationStatus.InProgress, reserva.Status);

        // Act: completar (usar Fin para cumplir la guarda utcNow >= EndDate)
        var completeHandler = new CompleteRentalCommandHandler(vehicles, reservations, unitOfWork, dueno, new FixedClock(Fin));
        await completeHandler.Handle(new CompleteRentalCommand(reservaId), CancellationToken.None);

        // Assert: estado Completed
        Assert.Equal(ReservationStatus.Completed, reserva.Status);
    }

    [Fact]
    public async Task RF_NEG_02_F5_inquilino_cancela_reserva_pendiente()
    {
        // Arrange: reserva directamente en Pending (sin confirmar)
        var vehicles = new InMemoryVehicleRepository();
        var reservations = new InMemoryReservationRepository();
        var unitOfWork = new WheelbyRecordingUnitOfWork();
        var inquilino = new MutableCurrentUser(Guid.NewGuid(), "Standard");
        var dueno = new MutableCurrentUser(Guid.NewGuid(), "Standard");
        var clock = new FixedClock(Ahora);

        var vehiculo = Vehicle.Create(dueno.Id, "Toyota", "Corolla", 2020, "ABC-123", 15000m, Ahora);
        await vehicles.AddAsync(vehiculo);

        var reserva = Reservation.Create(vehiculo.Id, inquilino.Id, Inicio, Fin, Ahora);
        // Ya nace en Pending, no hace falta confirmar
        await reservations.AddAsync(reserva);

        var cancelHandler = new CancelReservationCommandHandler(vehicles, reservations, unitOfWork, inquilino, new FixedClock(Ahora));

        // Act
        await cancelHandler.Handle(new CancelReservationCommand(reserva.Id, "Ya no lo necesito"), CancellationToken.None);

        // Assert
        Assert.Equal(ReservationStatus.Cancelled, reserva.Status);
        Assert.Equal("Ya no lo necesito", reserva.CancellationReason);
    }

    [Fact]
    public async Task RF_NEG_02_F5_tercero_que_no_participa_recibe_403()
    {
        // Arrange
        var (reservaId, vehicles, reservations, unitOfWork, _, _) = ConfigurarReservaConfirmada();
        var tercero = new MutableCurrentUser(Guid.NewGuid(), "Standard");

        var cancelHandler = new CancelReservationCommandHandler(vehicles, reservations, unitOfWork, tercero, new FixedClock(Ahora));

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(async () =>
            await cancelHandler.Handle(new CancelReservationCommand(reservaId, "motivo"), CancellationToken.None));
    }

    [Fact]
    public async Task RF_NEG_02_F5_cancelar_alquiler_en_curso_lanza_409()
    {
        // Arrange
        var (reservaId, vehicles, reservations, unitOfWork, dueno, _) = ConfigurarReservaConfirmada();
        var reserva = reservations.Reservations.First();
        reserva.TransitionTo(ReservationStatus.InProgress, ReservationActor.Host, Fin, null);

        var cancelHandler = new CancelReservationCommandHandler(vehicles, reservations, unitOfWork, dueno, new FixedClock(Fin));

        // Act & Assert: cancelar un alquiler en curso (InProgress -> Cancelled está prohibida)
        await Assert.ThrowsAsync<InvalidReservationTransitionException>(async () =>
            await cancelHandler.Handle(new CancelReservationCommand(reservaId, "motivo"), CancellationToken.None));

        Assert.Equal(ReservationStatus.InProgress, reserva.Status);
    }
}

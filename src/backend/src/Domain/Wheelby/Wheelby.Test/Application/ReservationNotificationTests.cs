using Microsoft.Extensions.Logging.Abstractions;
using Notifications.Contracts;
using Wheelby.Application.DTOs;
using Wheelby.Application.Features.NotifyReservationStatusChanged;
using Wheelby.Application.Features.RejectReservation;
using Wheelby.Domain.Reservations;
using Wheelby.Domain.Reservations.Events;
using Wheelby.Domain.Vehicles;
using Wheelby.Test.TestSupport;

// RF-NOT-03: cada transición de la reserva pasa por el motor de notificaciones.
// Quien actúa no recibe el aviso; el destinatario y el tipo salen de la tabla de CA-NOT-03.2.
public class ReservationNotificationTests
{
    private static readonly DateTime Ahora = new(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Inicio = new(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Fin = new(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid AnaId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid BetoId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003");

    private sealed record Escenario(
        NotifyReservationStatusChangedHandler Handler, SpyNotificationPublisher Publisher, ReservationSummaryDTO Reserva);

    // Ana es la anfitriona y Beto el inquilino.
    private static Escenario Crear()
    {
        var store = new InMemoryReservationReadStore();
        var reserva = new ReservationSummaryDTO(
            Guid.NewGuid(), Guid.NewGuid(), BetoId, AnaId, "Toyota", "Corolla", 2020, "ABC-123",
            Inicio, Fin, ReservationStatus.Pending, 1500m, 2, 3000m, null, Ahora);
        store.Agregar(reserva);
        var users = new FakeUserDirectory();
        users.Users[AnaId] = new AccessControl.Contracts.UserContact(AnaId, "Ana Pérez", "ana@wheelby.dev");
        users.Users[BetoId] = new AccessControl.Contracts.UserContact(BetoId, "Beto Gómez", "beto@wheelby.dev");
        var publisher = new SpyNotificationPublisher();
        var handler = new NotifyReservationStatusChangedHandler(
            store, users, publisher, NullLogger<NotifyReservationStatusChangedHandler>.Instance);
        return new Escenario(handler, publisher, reserva);
    }

    private static async Task<(NotificationMessage Message, IReadOnlyCollection<Guid> Recipients)> AvisarAsync(
        Escenario e, ReservationStatus? from, ReservationStatus to, ReservationActor actor, Guid? actorId,
        string? reason = null, bool isRejection = false)
    {
        await e.Handler.Handle(
            new ReservationStatusChanged(e.Reserva.Id, from, to, actor, actorId, reason, Ahora, isRejection),
            CancellationToken.None);
        return Assert.Single(e.Publisher.Published);
    }

    [Fact]
    public async Task RF_NOT_03_solicitar_reserva_notifica_al_anfitrion()
    {
        // Arrange
        var e = Crear();

        // Act
        var (message, recipients) = await AvisarAsync(e, null, ReservationStatus.Pending, ReservationActor.Renter, BetoId);

        // Assert
        Assert.Equal(new[] { AnaId }, recipients);
        Assert.Equal("reservation.requested", message.Type);
        Assert.Equal(e.Reserva.Id, message.ReferenceId);
        Assert.Contains("Toyota", message.Message);
        Assert.Contains("Corolla", message.Message);
        Assert.Contains("05/11/2026", message.Message);
        Assert.Contains("07/11/2026", message.Message);
        Assert.Contains("Beto Gómez", message.Message);
    }

    [Fact]
    public async Task RF_NOT_03_confirmar_notifica_al_inquilino()
    {
        // Arrange
        var e = Crear();

        // Act
        var (message, recipients) = await AvisarAsync(
            e, ReservationStatus.Pending, ReservationStatus.Confirmed, ReservationActor.Host, AnaId);

        // Assert
        Assert.Equal(new[] { BetoId }, recipients);
        Assert.Equal("reservation.confirmed", message.Type);
    }

    [Fact]
    public async Task RF_NOT_03_rechazar_notifica_al_inquilino_con_el_motivo()
    {
        // Arrange
        var e = Crear();

        // Act
        var (message, recipients) = await AvisarAsync(
            e, ReservationStatus.Pending, ReservationStatus.Cancelled, ReservationActor.Host, AnaId,
            "El vehículo está en el taller", isRejection: true);

        // Assert
        Assert.Equal(new[] { BetoId }, recipients);
        Assert.Equal("reservation.rejected", message.Type);
        Assert.Contains("El vehículo está en el taller", message.Message);
    }

    [Theory]
    [InlineData(ReservationStatus.Pending)]
    [InlineData(ReservationStatus.Confirmed)]
    public async Task RF_NOT_03_cancelar_inquilino_notifica_al_anfitrion(ReservationStatus desde)
    {
        // Arrange
        var e = Crear();

        // Act
        var (message, recipients) = await AvisarAsync(e, desde, ReservationStatus.Cancelled, ReservationActor.Renter, BetoId);

        // Assert
        Assert.Equal(new[] { AnaId }, recipients);
        Assert.Equal("reservation.cancelled", message.Type);
    }

    [Theory]
    [InlineData(ReservationStatus.Pending)]
    [InlineData(ReservationStatus.Confirmed)]
    public async Task RF_NOT_03_cancelar_anfitrion_notifica_al_inquilino_con_el_motivo(ReservationStatus desde)
    {
        // Arrange
        var e = Crear();

        // Act
        var (message, recipients) = await AvisarAsync(
            e, desde, ReservationStatus.Cancelled, ReservationActor.Host, AnaId, "Surgió un imprevisto");

        // Assert
        Assert.Equal(new[] { BetoId }, recipients);
        Assert.Equal("reservation.cancelled", message.Type);
        Assert.Contains("Surgió un imprevisto", message.Message);
    }

    [Fact]
    public async Task RF_NOT_03_expirar_notifica_al_anfitrion_y_al_inquilino()
    {
        // Arrange
        var e = Crear();

        // Act
        var (message, recipients) = await AvisarAsync(
            e, ReservationStatus.Pending, ReservationStatus.Cancelled, ReservationActor.System, null, "La solicitud expiró");

        // Assert
        Assert.Equal(new[] { AnaId, BetoId }.OrderBy(x => x), recipients.OrderBy(x => x));
        Assert.Equal("reservation.expired", message.Type);
    }

    [Fact]
    public async Task RF_NOT_03_iniciar_notifica_al_inquilino()
    {
        // Arrange
        var e = Crear();

        // Act
        var (message, recipients) = await AvisarAsync(
            e, ReservationStatus.Confirmed, ReservationStatus.InProgress, ReservationActor.Host, AnaId);

        // Assert
        Assert.Equal(new[] { BetoId }, recipients);
        Assert.Equal("reservation.started", message.Type);
    }

    [Fact]
    public async Task RF_NOT_03_completar_notifica_al_inquilino()
    {
        // Arrange
        var e = Crear();

        // Act
        var (message, recipients) = await AvisarAsync(
            e, ReservationStatus.InProgress, ReservationStatus.Completed, ReservationActor.Host, AnaId);

        // Assert
        Assert.Equal(new[] { BetoId }, recipients);
        Assert.Equal("reservation.completed", message.Type);
    }

    [Fact]
    public async Task RF_NOT_03_rechazar_marca_el_evento_como_rechazo()
    {
        // Arrange
        var vehicles = new InMemoryVehicleRepository();
        var reservations = new InMemoryReservationRepository();
        var vehiculo = Vehicle.Create(AnaId, "Toyota", "Corolla", 2020, "ABC-123", 1500m, Ahora);
        await vehicles.AddAsync(vehiculo);
        var reserva = Reservation.Create(vehiculo.Id, BetoId, Inicio, Fin, 1500m, Ahora);
        reserva.ClearDomainEvents();
        await reservations.AddAsync(reserva);
        var handler = new RejectReservationCommandHandler(
            vehicles, reservations, new WheelbyRecordingUnitOfWork(), new MutableCurrentUser(AnaId), new FixedClock(Ahora));

        // Act
        await handler.Handle(new RejectReservationCommand(reserva.Id, "No disponible"), CancellationToken.None);

        // Assert
        var evento = Assert.IsType<ReservationStatusChanged>(Assert.Single(reserva.DomainEvents));
        Assert.True(evento.IsRejection);
        Assert.Equal(ReservationStatus.Cancelled, evento.To);
    }

    [Fact]
    public void RF_NOT_03_cancelar_no_marca_el_evento_como_rechazo()
    {
        // Arrange
        var reserva = Reservation.Create(Guid.NewGuid(), BetoId, Inicio, Fin, 1500m, Ahora);
        reserva.ClearDomainEvents();

        // Act
        reserva.TransitionTo(ReservationStatus.Cancelled, ReservationActor.Host, Ahora, "Imprevisto", AnaId);

        // Assert
        var evento = Assert.IsType<ReservationStatusChanged>(Assert.Single(reserva.DomainEvents));
        Assert.False(evento.IsRejection);
    }

    [Fact]
    public void RF_NOT_03_los_avisos_de_reserva_ya_no_usan_la_cola_directa()
    {
        // Arrange
        var parametros = typeof(NotifyReservationStatusChangedHandler)
            .GetConstructors().Single()
            .GetParameters()
            .Select(p => p.ParameterType)
            .ToList();

        // Act y Assert
        Assert.Contains(typeof(INotificationPublisher), parametros);
        Assert.DoesNotContain(typeof(IEmailQueue), parametros);
    }
}

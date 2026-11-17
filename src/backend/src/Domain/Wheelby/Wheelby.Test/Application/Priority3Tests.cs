using Microsoft.Extensions.Logging.Abstractions;
using SharedKernel.Exceptions;
using Wheelby.Application.DTOs;
using Wheelby.Application.Features.AttachReservationDocument;
using Wheelby.Application.Features.ExpirePendingReservations;
using Wheelby.Application.Features.GetReservationDocumentContent;
using Wheelby.Application.Features.NotifyReservationStatusChanged;
using Wheelby.Domain.Errors;
using Wheelby.Domain.Reservations;
using Wheelby.Domain.Reservations.Events;
using Wheelby.Domain.Vehicles;
using Wheelby.Test.TestSupport;

// Prioridad 3: historial, notificaciones, documentos de la reserva y expiración.
public class Priority3Tests
{
    private static readonly DateTime Ahora = new(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Inicio = new(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Fin = new(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc);

    private sealed record Escenario(
        InMemoryVehicleRepository Vehicles,
        InMemoryReservationRepository Reservations,
        WheelbyRecordingUnitOfWork UnitOfWork,
        Vehicle Vehiculo,
        Reservation Reserva,
        Guid Dueno,
        Guid Inquilino);

    private static async Task<Escenario> Crear(bool confirmar = true)
    {
        var vehicles = new InMemoryVehicleRepository();
        var reservations = new InMemoryReservationRepository();
        var dueno = Guid.NewGuid();
        var inquilino = Guid.NewGuid();
        var vehiculo = Vehicle.Create(dueno, "Toyota", "Corolla", 2020, "ABC-123", 1500m, Ahora);
        await vehicles.AddAsync(vehiculo);
        var reserva = Reservation.Create(vehiculo.Id, inquilino, Inicio, Fin, 1500m, Ahora);
        if (confirmar)
            reserva.TransitionTo(ReservationStatus.Confirmed, ReservationActor.Host, Ahora, actorId: dueno);
        await reservations.AddAsync(reserva);
        return new Escenario(vehicles, reservations, new WheelbyRecordingUnitOfWork(), vehiculo, reserva, dueno, inquilino);
    }

    // ---------- Historial y eventos (dominio)

    [Fact]
    public void Crear_y_transicionar_registra_historial_y_eventos()
    {
        var renter = Guid.NewGuid();
        var host = Guid.NewGuid();
        var reserva = Reservation.Create(Guid.NewGuid(), renter, Inicio, Fin, 100m, Ahora);
        reserva.TransitionTo(ReservationStatus.Cancelled, ReservationActor.Host, Ahora, "  Sin disponibilidad ", host);

        Assert.Collection(reserva.History,
            h => { Assert.Null(h.From); Assert.Equal(ReservationStatus.Pending, h.To); Assert.Equal(renter, h.ActorId); },
            h =>
            {
                Assert.Equal(ReservationStatus.Pending, h.From);
                Assert.Equal(ReservationStatus.Cancelled, h.To);
                Assert.Equal(ReservationActor.Host, h.Actor);
                Assert.Equal(host, h.ActorId);
                Assert.Equal("Sin disponibilidad", h.Reason);
            });
        Assert.All(reserva.History, h => Assert.Equal(reserva.Id, h.ReservationId));
        Assert.Equal(2, reserva.DomainEvents.OfType<ReservationStatusChanged>().Count());
    }

    [Fact]
    public void Transicion_rechazada_no_agrega_historial()
    {
        var reserva = Reservation.Create(Guid.NewGuid(), Guid.NewGuid(), Inicio, Fin, 100m, Ahora);

        Assert.ThrowsAny<AppException>(() =>
            reserva.TransitionTo(ReservationStatus.Completed, ReservationActor.Host, Ahora));

        Assert.Single(reserva.History);
        Assert.Single(reserva.DomainEvents);
    }

    // ---------- Expiración

    [Fact]
    public void System_no_expira_antes_del_inicio_ni_cancela_confirmadas()
    {
        var pendiente = Reservation.Create(Guid.NewGuid(), Guid.NewGuid(), Inicio, Fin, 100m, Ahora);
        Assert.Throws<Wheelby.Domain.Reservations.Exceptions.ReservationTransitionNotAllowedException>(() =>
            pendiente.TransitionTo(ReservationStatus.Cancelled, ReservationActor.System, Inicio.AddMinutes(-1)));

        var confirmada = Reservation.Create(Guid.NewGuid(), Guid.NewGuid(), Inicio, Fin, 100m, Ahora);
        confirmada.TransitionTo(ReservationStatus.Confirmed, ReservationActor.Host, Ahora);
        Assert.Throws<Wheelby.Domain.Reservations.Exceptions.ReservationTransitionNotAllowedException>(() =>
            confirmada.TransitionTo(ReservationStatus.Cancelled, ReservationActor.System, Inicio));
    }

    [Fact]
    public async Task Expire_cancela_solo_las_pendientes_vencidas()
    {
        var e = await Crear(confirmar: false);
        var futura = Reservation.Create(e.Vehiculo.Id, Guid.NewGuid(), Fin, Fin.AddDays(1), 100m, Ahora);
        await e.Reservations.AddAsync(futura);

        var handler = new ExpirePendingReservationsCommandHandler(e.Reservations, e.UnitOfWork, new FixedClock(Inicio));
        var expiradas = await handler.Handle(new ExpirePendingReservationsCommand(), CancellationToken.None);

        Assert.Equal(1, expiradas);
        Assert.Equal(ReservationStatus.Cancelled, e.Reserva.Status);
        Assert.Equal(ExpirePendingReservationsCommandHandler.Reason, e.Reserva.CancellationReason);
        Assert.Equal(ReservationActor.System, e.Reserva.History.Last().Actor);
        Assert.Null(e.Reserva.History.Last().ActorId);
        Assert.Equal(ReservationStatus.Pending, futura.Status);
        Assert.Equal(1, e.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Expire_sin_vencidas_no_guarda()
    {
        var e = await Crear(confirmar: false);
        var handler = new ExpirePendingReservationsCommandHandler(e.Reservations, e.UnitOfWork, new FixedClock(Ahora));

        Assert.Equal(0, await handler.Handle(new ExpirePendingReservationsCommand(), CancellationToken.None));
        Assert.Equal(0, e.UnitOfWork.SaveCount);
    }

    // ---------- Notificaciones

    [Theory]
    [InlineData(ReservationActor.Renter, "dueno")]
    [InlineData(ReservationActor.Host, "inquilino")]
    [InlineData(ReservationActor.System, "ambos")]
    public void Avisa_a_la_contraparte_de_quien_actua(ReservationActor actor, string esperado)
    {
        var renter = Guid.NewGuid();
        var owner = Guid.NewGuid();

        var destinatarios = NotifyReservationStatusChangedHandler.Recipients(actor, renter, owner).ToList();

        Guid[] expected = esperado switch
        {
            "dueno" => [owner],
            "inquilino" => [renter],
            _ => [renter, owner]
        };
        Assert.Equal(expected, destinatarios);
    }

    [Theory]
    [InlineData(null, ReservationStatus.Pending, ReservationActor.Renter, "Requested")]
    [InlineData(ReservationStatus.Pending, ReservationStatus.Confirmed, ReservationActor.Host, "Confirmed")]
    [InlineData(ReservationStatus.Pending, ReservationStatus.Cancelled, ReservationActor.Host, "Rejected")]
    [InlineData(ReservationStatus.Pending, ReservationStatus.Cancelled, ReservationActor.Renter, "CancelledByRenter")]
    [InlineData(ReservationStatus.Confirmed, ReservationStatus.Cancelled, ReservationActor.Host, "CancelledByHost")]
    [InlineData(ReservationStatus.Pending, ReservationStatus.Cancelled, ReservationActor.System, "Expired")]
    [InlineData(ReservationStatus.Confirmed, ReservationStatus.InProgress, ReservationActor.Host, "Started")]
    [InlineData(ReservationStatus.InProgress, ReservationStatus.Completed, ReservationActor.Host, "Completed")]
    public void Cada_transicion_tiene_su_aviso(
        ReservationStatus? from, ReservationStatus to, ReservationActor actor, string esperado)
    {
        var evento = new ReservationStatusChanged(Guid.NewGuid(), from, to, actor, null, null, Ahora);
        Assert.Equal(esperado, ReservationEmailTemplates.Kind(evento).ToString());
    }

    [Fact]
    public async Task El_handler_encola_el_correo_del_anfitrion_al_solicitar()
    {
        var (store, reserva) = ReadStoreCon();
        var users = new FakeUserDirectory();
        users.Agregar(reserva.OwnerId, "host@w.dev");
        users.Agregar(reserva.RenterId, "renter@w.dev");
        var queue = new FakeEmailQueue();
        var handler = new NotifyReservationStatusChangedHandler(store, users, queue, NullLogger<NotifyReservationStatusChangedHandler>.Instance);

        await handler.Handle(new ReservationStatusChanged(
            reserva.Id, null, ReservationStatus.Pending, ReservationActor.Renter, reserva.RenterId, null, Ahora), CancellationToken.None);

        var correo = Assert.Single(queue.Sent);
        Assert.Equal("host@w.dev", correo.To);
        Assert.Contains("Nueva solicitud", correo.Subject);
        Assert.Contains("Toyota Corolla", correo.Body);
    }

    [Fact]
    public async Task Si_la_cola_falla_el_handler_no_propaga_la_excepcion()
    {
        var (store, reserva) = ReadStoreCon();
        var users = new FakeUserDirectory();
        users.Agregar(reserva.RenterId, "renter@w.dev");
        var queue = new FakeEmailQueue { FailWith = new BadRequestException("correo inválido") };
        var handler = new NotifyReservationStatusChangedHandler(store, users, queue, NullLogger<NotifyReservationStatusChangedHandler>.Instance);

        await handler.Handle(new ReservationStatusChanged(
            reserva.Id, ReservationStatus.Pending, ReservationStatus.Confirmed, ReservationActor.Host, reserva.OwnerId, null, Ahora),
            CancellationToken.None);
    }

    private static (InMemoryReservationReadStore, ReservationSummaryDTO) ReadStoreCon()
    {
        var store = new InMemoryReservationReadStore();
        var dto = new ReservationSummaryDTO(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Toyota", "Corolla", 2020, "ABC-123",
            Inicio, Fin, ReservationStatus.Pending, 1500m, 2, 3000m, null, Ahora);
        store.Agregar(dto);
        return (store, dto);
    }

    // ---------- Documentos de la reserva

    [Fact]
    public async Task Participante_adjunta_un_documento_propio()
    {
        var e = await Crear();
        var docs = new FakeDocumentAccess();
        var docId = docs.Agregar(e.Inquilino, "application/pdf");
        var handler = new AttachReservationDocumentCommandHandler(
            e.Reservations, e.Vehicles, docs, e.UnitOfWork, new MutableCurrentUser(e.Inquilino), new FixedClock(Ahora));

        await handler.Handle(new AttachReservationDocumentCommand(e.Reserva.Id, docId, ReservationDocumentKind.Delivery), CancellationToken.None);

        var adjunto = Assert.Single(e.Reservations.Documents);
        Assert.Equal(docId, adjunto.DocumentId);
        Assert.Equal(ReservationDocumentKind.Delivery, adjunto.Kind);
        Assert.Equal(e.Inquilino, adjunto.AttachedBy);
    }

    [Fact]
    public async Task Adjuntar_reglas_de_rechazo()
    {
        var e = await Crear();
        var docs = new FakeDocumentAccess();
        var propio = docs.Agregar(e.Dueno);
        var ajeno = docs.Agregar(e.Inquilino);
        var tercero = Guid.NewGuid();

        AttachReservationDocumentCommandHandler Para(Guid user) => new(
            e.Reservations, e.Vehicles, docs, e.UnitOfWork, new MutableCurrentUser(user), new FixedClock(Ahora));

        var noParticipa = await Assert.ThrowsAsync<NotFoundException>(() => Para(tercero).Handle(
            new AttachReservationDocumentCommand(e.Reserva.Id, propio, ReservationDocumentKind.Delivery), CancellationToken.None));
        Assert.Equal(WheelbyErrorCodes.ReservationNotFound, noParticipa.Code);

        var noEsSuyo = await Assert.ThrowsAsync<ForbiddenException>(() => Para(e.Dueno).Handle(
            new AttachReservationDocumentCommand(e.Reserva.Id, ajeno, ReservationDocumentKind.Delivery), CancellationToken.None));
        Assert.Equal(WheelbyErrorCodes.DocumentNotOwner, noEsSuyo.Code);

        await Para(e.Dueno).Handle(new AttachReservationDocumentCommand(e.Reserva.Id, propio, ReservationDocumentKind.Return), CancellationToken.None);
        var duplicado = await Assert.ThrowsAsync<ConflictException>(() => Para(e.Dueno).Handle(
            new AttachReservationDocumentCommand(e.Reserva.Id, propio, ReservationDocumentKind.Return), CancellationToken.None));
        Assert.Equal(WheelbyErrorCodes.ReservationDocumentDuplicate, duplicado.Code);
    }

    [Fact]
    public async Task No_se_adjunta_a_una_reserva_pendiente()
    {
        var e = await Crear(confirmar: false);
        var docs = new FakeDocumentAccess();
        var doc = docs.Agregar(e.Inquilino);
        var handler = new AttachReservationDocumentCommandHandler(
            e.Reservations, e.Vehicles, docs, e.UnitOfWork, new MutableCurrentUser(e.Inquilino), new FixedClock(Ahora));

        var ex = await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new AttachReservationDocumentCommand(e.Reserva.Id, doc, ReservationDocumentKind.Delivery), CancellationToken.None));
        Assert.Equal(WheelbyErrorCodes.ReservationDocumentNotAllowed, ex.Code);
    }

    [Fact]
    public async Task El_contenido_solo_se_sirve_si_el_documento_esta_adjunto()
    {
        var e = await Crear();
        var docs = new FakeDocumentAccess();
        var suelto = docs.Agregar(e.Dueno);
        var store = new InMemoryReservationReadStore();
        store.Agregar(new ReservationSummaryDTO(
            e.Reserva.Id, e.Vehiculo.Id, e.Inquilino, e.Dueno, "Toyota", "Corolla", 2020, "ABC-123",
            Inicio, Fin, ReservationStatus.Confirmed, 1500m, 2, 3000m, null, Ahora));

        var handler = new GetReservationDocumentContentQueryHandler(
            store, e.Reservations, docs, new MutableCurrentUser(e.Inquilino));

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new GetReservationDocumentContentQuery(e.Reserva.Id, suelto), CancellationToken.None));
        Assert.Equal(WheelbyErrorCodes.DocumentNotFound, ex.Code);

        await e.Reservations.AddDocumentAsync(
            ReservationDocument.Attach(e.Reserva, suelto, ReservationDocumentKind.Delivery, e.Dueno, Ahora));
        var archivo = await handler.Handle(new GetReservationDocumentContentQuery(e.Reserva.Id, suelto), CancellationToken.None);
        Assert.Equal("image/jpeg", archivo.ContentType);
    }
}

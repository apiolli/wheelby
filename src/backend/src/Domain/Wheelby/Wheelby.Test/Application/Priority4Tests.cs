using SharedKernel.Exceptions;
using Wheelby.Application.Features.AddVehiclePhoto;
using Wheelby.Application.Features.CreateReview;
using Wheelby.Application.Features.GetVehiclePhotoContent;
using Wheelby.Application.Features.RequestReservation;
using Wheelby.Application.Features.SetVehicleActive;
using Wheelby.Application.Features.UpdateVehicle;
using Wheelby.Domain.Errors;
using Wheelby.Domain.Reservations;
using Wheelby.Domain.Vehicles;
using Wheelby.Test.TestSupport;

// Prioridad 4: editar y desactivar vehículos, fotos y reseñas.
public class Priority4Tests
{
    private static readonly DateTime Ahora = new(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Inicio = new(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Fin = new(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc);

    private static async Task<(InMemoryVehicleRepository Repo, Vehicle Vehiculo)> UnVehiculo(Guid dueno, string placa = "ABC-123")
    {
        var repo = new InMemoryVehicleRepository();
        var v = Vehicle.Create(dueno, "Toyota", "Corolla", 2020, placa, 1500m, Ahora, VehicleType.Car, "Santiago");
        await repo.AddAsync(v);
        return (repo, v);
    }

    private static UpdateVehicleCommand Edicion(Guid id, string placa = "XYZ-999", decimal precio = 2000m)
        => new(id, "Honda", "Civic", 2022, placa, precio, VehicleType.Car, " Santo Domingo ", "Muy cuidado");

    // ---------- Editar

    [Fact]
    public async Task El_dueno_edita_y_el_total_de_reservas_existentes_no_cambia()
    {
        var dueno = Guid.NewGuid();
        var (repo, v) = await UnVehiculo(dueno);
        var reserva = Reservation.Create(v.Id, Guid.NewGuid(), Inicio, Fin, v.PricePerDay, Ahora);

        var handler = new UpdateVehicleCommandHandler(repo, new WheelbyRecordingUnitOfWork(), new MutableCurrentUser(dueno), new FixedClock(Ahora));
        await handler.Handle(Edicion(v.Id), CancellationToken.None);

        Assert.Equal("Honda", v.Brand);
        Assert.Equal("XYZ-999", v.Plate);
        Assert.Equal("Santo Domingo", v.City);
        Assert.Equal(2000m, v.PricePerDay);
        Assert.Equal(3000m, reserva.TotalAmount);
    }

    [Fact]
    public async Task Editar_reglas_de_rechazo()
    {
        var dueno = Guid.NewGuid();
        var (repo, v) = await UnVehiculo(dueno);
        await repo.AddAsync(Vehicle.Create(Guid.NewGuid(), "Kia", "Rio", 2021, "OTRA-1", 900m, Ahora));

        var ajeno = new UpdateVehicleCommandHandler(repo, new WheelbyRecordingUnitOfWork(), new MutableCurrentUser(Guid.NewGuid()), new FixedClock(Ahora));
        var noDueno = await Assert.ThrowsAsync<ForbiddenException>(() => ajeno.Handle(Edicion(v.Id), CancellationToken.None));
        Assert.Equal(WheelbyErrorCodes.VehicleNotOwner, noDueno.Code);

        var propio = new UpdateVehicleCommandHandler(repo, new WheelbyRecordingUnitOfWork(), new MutableCurrentUser(dueno), new FixedClock(Ahora));
        var placa = await Assert.ThrowsAsync<ConflictException>(() => propio.Handle(Edicion(v.Id, "otra-1"), CancellationToken.None));
        Assert.Equal(WheelbyErrorCodes.VehiclePlateTaken, placa.Code);

        // Conservar su propia placa no es conflicto.
        await propio.Handle(Edicion(v.Id, "ABC-123"), CancellationToken.None);
    }

    // ---------- Desactivar

    [Fact]
    public async Task No_se_desactiva_con_reservas_confirmadas_y_un_inactivo_no_se_reserva()
    {
        var dueno = Guid.NewGuid();
        var (repo, v) = await UnVehiculo(dueno);
        var reservas = new InMemoryReservationRepository();
        var confirmada = Reservation.Create(v.Id, Guid.NewGuid(), Inicio, Fin, 100m, Ahora);
        confirmada.TransitionTo(ReservationStatus.Confirmed, ReservationActor.Host, Ahora);
        await reservas.AddAsync(confirmada);

        var handler = new SetVehicleActiveCommandHandler(repo, reservas, new WheelbyRecordingUnitOfWork(), new MutableCurrentUser(dueno), new FixedClock(Ahora));
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new SetVehicleActiveCommand(v.Id, false), CancellationToken.None));
        Assert.Equal(WheelbyErrorCodes.VehicleHasActiveReservations, ex.Code);
        Assert.True(v.IsActive);

        confirmada.TransitionTo(ReservationStatus.Cancelled, ReservationActor.Renter, Ahora);
        await handler.Handle(new SetVehicleActiveCommand(v.Id, false), CancellationToken.None);
        Assert.False(v.IsActive);

        var request = new RequestReservationCommandHandler(repo, reservas, new WheelbyRecordingUnitOfWork(),
            new MutableCurrentUser(Guid.NewGuid()), new FixedClock(Ahora));
        var inactivo = await Assert.ThrowsAsync<ConflictException>(() =>
            request.Handle(new RequestReservationCommand(v.Id, Inicio, Fin), CancellationToken.None));
        Assert.Equal(WheelbyErrorCodes.VehicleInactive, inactivo.Code);

        await handler.Handle(new SetVehicleActiveCommand(v.Id, true), CancellationToken.None);
        Assert.True(v.IsActive);
    }

    // ---------- Fotos

    [Fact]
    public async Task Fotos_solo_imagenes_propias_y_hasta_el_limite()
    {
        var dueno = Guid.NewGuid();
        var (repo, v) = await UnVehiculo(dueno);
        var docs = new FakeDocumentAccess();
        var handler = new AddVehiclePhotoCommandHandler(repo, docs, new WheelbyRecordingUnitOfWork(), new MutableCurrentUser(dueno), new FixedClock(Ahora));

        var pdf = docs.Agregar(dueno, "application/pdf");
        var noImagen = await Assert.ThrowsAsync<BadRequestException>(() =>
            handler.Handle(new AddVehiclePhotoCommand(v.Id, pdf), CancellationToken.None));
        Assert.Equal(WheelbyErrorCodes.VehiclePhotoInvalid, noImagen.Code);

        var ajena = docs.Agregar(Guid.NewGuid());
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new AddVehiclePhotoCommand(v.Id, ajena), CancellationToken.None));

        for (var i = 0; i < VehiclePhoto.MaxPerVehicle; i++)
            await handler.Handle(new AddVehiclePhotoCommand(v.Id, docs.Agregar(dueno)), CancellationToken.None);

        var limite = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new AddVehiclePhotoCommand(v.Id, docs.Agregar(dueno)), CancellationToken.None));
        Assert.Equal(WheelbyErrorCodes.VehiclePhotoLimit, limite.Code);
    }

    [Fact]
    public async Task La_foto_solo_se_sirve_si_esta_vinculada_al_vehiculo()
    {
        var dueno = Guid.NewGuid();
        var (repo, v) = await UnVehiculo(dueno);
        var docs = new FakeDocumentAccess();
        var foto = docs.Agregar(dueno);
        var suelto = docs.Agregar(dueno);
        await repo.AddPhotoAsync(new VehiclePhoto(v.Id, foto, Ahora));

        var handler = new GetVehiclePhotoContentQueryHandler(repo, docs);

        var archivo = await handler.Handle(new GetVehiclePhotoContentQuery(v.Id, foto), CancellationToken.None);
        Assert.Equal("image/jpeg", archivo.ContentType);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetVehiclePhotoContentQuery(v.Id, suelto), CancellationToken.None));
    }

    // ---------- Reseñas

    private static async Task<(CreateReviewCommandHandler Para, Reservation Reserva, Guid Dueno, Guid Inquilino, InMemoryReviewRepository Reviews)>
        Resenable(Guid usuario, bool completar = true, Guid? dueno = null, Guid? inquilino = null)
    {
        var d = dueno ?? Guid.NewGuid();
        var r = inquilino ?? Guid.NewGuid();
        var (repo, v) = await UnVehiculo(d);
        var reservas = new InMemoryReservationRepository();
        var reserva = Reservation.Create(v.Id, r, Inicio, Fin, 100m, Ahora);
        reserva.TransitionTo(ReservationStatus.Confirmed, ReservationActor.Host, Ahora);
        reserva.TransitionTo(ReservationStatus.InProgress, ReservationActor.Host, Inicio);
        if (completar)
            reserva.TransitionTo(ReservationStatus.Completed, ReservationActor.Host, Fin);
        await reservas.AddAsync(reserva);
        var reviews = new InMemoryReviewRepository();
        var handler = new CreateReviewCommandHandler(reservas, repo, reviews, new WheelbyRecordingUnitOfWork(),
            new MutableCurrentUser(usuario), new FixedClock(Fin));
        return (handler, reserva, d, r, reviews);
    }

    [Fact]
    public async Task Cada_participante_resena_una_vez_y_se_asigna_el_sujeto()
    {
        var inquilino = Guid.NewGuid();
        var dueno = Guid.NewGuid();
        var (handler, reserva, _, _, reviews) = await Resenable(inquilino, dueno: dueno, inquilino: inquilino);

        await handler.Handle(new CreateReviewCommand(reserva.Id, 5, " Excelente "), CancellationToken.None);

        var review = Assert.Single(reviews.Reviews);
        Assert.Equal(ReservationActor.Renter, review.AuthorRole);
        Assert.Equal(dueno, review.SubjectId);
        Assert.Equal("Excelente", review.Comment);

        var duplicada = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new CreateReviewCommand(reserva.Id, 4, null), CancellationToken.None));
        Assert.Equal(WheelbyErrorCodes.ReviewDuplicate, duplicada.Code);
    }

    [Fact]
    public async Task Resenar_reglas_de_rechazo()
    {
        var inquilino = Guid.NewGuid();
        var (enCurso, reservaEnCurso, _, _, _) = await Resenable(inquilino, completar: false, inquilino: inquilino);
        var noCompletada = await Assert.ThrowsAsync<ConflictException>(() =>
            enCurso.Handle(new CreateReviewCommand(reservaEnCurso.Id, 5, null), CancellationToken.None));
        Assert.Equal(WheelbyErrorCodes.ReviewNotAllowed, noCompletada.Code);

        var (tercero, reserva, _, _, _) = await Resenable(Guid.NewGuid());
        await Assert.ThrowsAsync<NotFoundException>(() =>
            tercero.Handle(new CreateReviewCommand(reserva.Id, 5, null), CancellationToken.None));

        Assert.False(new CreateReviewCommandValidator().Validate(new CreateReviewCommand(reserva.Id, 6, null)).IsValid);
    }
}

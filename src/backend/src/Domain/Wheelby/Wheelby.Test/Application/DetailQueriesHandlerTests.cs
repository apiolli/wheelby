using SharedKernel.Exceptions;
using Wheelby.Application.DTOs;
using Wheelby.Application.Features.GetReservation;
using Wheelby.Application.Features.GetVehicle;
using Wheelby.Application.Features.GetVehicleAvailability;
using Wheelby.Application.Features.ListMyVehicles;
using Wheelby.Domain.Errors;
using Wheelby.Domain.Reservations;
using Wheelby.Test.TestSupport;

// Prioridad 1: detalle de reserva, ficha del vehículo, mis vehículos y disponibilidad.
public class DetailQueriesHandlerTests
{
    private static readonly Guid Inquilino = Guid.NewGuid();
    private static readonly Guid Anfitrion = Guid.NewGuid();
    private static readonly Guid Extrano = Guid.NewGuid();
    private static readonly DateTime D5 = new(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime D7 = new(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime D9 = new(2026, 11, 9, 10, 0, 0, DateTimeKind.Utc);

    private static (InMemoryReservationReadStore Store, Guid Id) UnaReserva()
    {
        var store = new InMemoryReservationReadStore();
        var id = Guid.NewGuid();
        store.Agregar(new ReservationSummaryDTO(
            id, Guid.NewGuid(), Inquilino, Anfitrion, "Toyota", "Corolla", 2020, "A123456",
            D5, D7, ReservationStatus.Pending, 100m, 2, 200m, null, D5.AddDays(-3)));
        return (store, id);
    }

    [Theory]
    [MemberData(nameof(QuienPuedeVer))]
    public async Task GetReservation_lo_ven_inquilino_anfitrion_y_administrador(Guid usuario, string rol)
    {
        var (store, id) = UnaReserva();
        var handler = new GetReservationQueryHandler(store, new MutableCurrentUser(usuario, rol));

        var reserva = await handler.Handle(new GetReservationQuery(id), CancellationToken.None);

        Assert.Equal(id, reserva.Id);
        Assert.Equal(200m, reserva.TotalAmount);
    }

    public static TheoryData<Guid, string> QuienPuedeVer() => new()
    {
        { Inquilino, AccessControlRoles.Standard },
        { Anfitrion, AccessControlRoles.Standard },
        { Extrano, AccessControlRoles.Administrator },
    };

    [Fact]
    public async Task GetReservation_un_tercero_recibe_404_sin_revelar_existencia()
    {
        var (store, id) = UnaReserva();
        var handler = new GetReservationQueryHandler(store, new MutableCurrentUser(Extrano, AccessControlRoles.Standard));

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetReservationQuery(id), CancellationToken.None));
        var noExiste = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetReservationQuery(Guid.NewGuid()), CancellationToken.None));

        // Misma respuesta exista o no la reserva.
        Assert.Equal(noExiste.Message, ex.Message);
        Assert.Equal(WheelbyErrorCodes.ReservationNotFound, ex.Code);
    }

    [Fact]
    public async Task GetVehicle_devuelve_la_ficha_o_404()
    {
        var store = new InMemoryVehicleReadStore();
        var id = Guid.NewGuid();
        store.AgregarVehiculo(id, Anfitrion, "ABC-001");
        var handler = new GetVehicleQueryHandler(store);

        var vehiculo = await handler.Handle(new GetVehicleQuery(id), CancellationToken.None);
        Assert.Equal("ABC-001", vehiculo.Plate);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetVehicleQuery(Guid.NewGuid()), CancellationToken.None));
        Assert.Equal(WheelbyErrorCodes.VehicleNotFound, ex.Code);
    }

    [Fact]
    public async Task ListMyVehicles_devuelve_solo_los_del_anfitrion()
    {
        var store = new InMemoryVehicleReadStore();
        store.AgregarVehiculo(Guid.NewGuid(), Anfitrion, "ABC-001");
        store.AgregarVehiculo(Guid.NewGuid(), Anfitrion, "ABC-002");
        store.AgregarVehiculo(Guid.NewGuid(), Extrano, "ABC-003");
        var handler = new ListMyVehiclesQueryHandler(store, new MutableCurrentUser(Anfitrion));

        var pagina = await handler.Handle(new ListMyVehiclesQuery(), CancellationToken.None);

        Assert.Equal(2, pagina.TotalItems);
        Assert.All(pagina.Items, v => Assert.Equal(Anfitrion, v.OwnerId));
    }

    [Fact]
    public async Task Availability_devuelve_solo_rangos_bloqueantes_que_tocan_el_intervalo()
    {
        var store = new InMemoryVehicleReadStore();
        var vehiculo = Guid.NewGuid();
        store.AgregarVehiculo(vehiculo, Anfitrion, "ABC-001");
        store.AgregarReserva(vehiculo, ReservationStatus.Confirmed, D5, D7);
        store.AgregarReserva(vehiculo, ReservationStatus.Pending, D7, D9);     // no bloquea
        store.AgregarReserva(vehiculo, ReservationStatus.InProgress, D9, D9.AddDays(1)); // fuera del rango
        var handler = new GetVehicleAvailabilityQueryHandler(store);

        var disponibilidad = await handler.Handle(
            new GetVehicleAvailabilityQuery(vehiculo, D5.AddDays(-1), D9), CancellationToken.None);

        var rango = Assert.Single(disponibilidad.Blocked);
        Assert.Equal(D5, rango.StartDate);
        Assert.Equal(D7, rango.EndDate);
    }

    [Fact]
    public async Task Availability_de_vehiculo_inexistente_es_404()
    {
        var handler = new GetVehicleAvailabilityQueryHandler(new InMemoryVehicleReadStore());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetVehicleAvailabilityQuery(Guid.NewGuid(), D5, D7), CancellationToken.None));
    }

    [Fact]
    public void Availability_validador_rechaza_rango_invertido()
    {
        var result = new GetVehicleAvailabilityQueryValidator()
            .Validate(new GetVehicleAvailabilityQuery(Guid.NewGuid(), D7, D5));

        Assert.False(result.IsValid);
    }
}

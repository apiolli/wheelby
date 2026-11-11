using Wheelby.Application.Features.ListAllReservations;
using Wheelby.Application.Features.ListMyReservations;
using Wheelby.Application.Abstractions;
using Wheelby.Application.DTOs;
using Wheelby.Domain.Reservations;
using Wheelby.Test.TestSupport;

// F6: Consultas de reservas.
public class ReservationQueriesHandlerTests
{
    private static readonly Guid UsuarioA = Guid.NewGuid();
    private static readonly Guid UsuarioB = Guid.NewGuid();

    private static ReservationSummaryDTO NuevaReserva(
        Guid id, Guid vehicleId, Guid renterId, DateTime start, DateTime end,
        ReservationStatus status, DateTime createdAt)
        => new(id, vehicleId, renterId, start, end, status, createdAt);

    [Fact]
    public async Task RF_NEG_02_F6_ListMyReservations_devuelve_solo_las_del_usuario()
    {
        // Arrange
        var readStore = new InMemoryReservationReadStore();
        readStore.Agregar(NuevaReserva(
            Guid.NewGuid(), Guid.NewGuid(), UsuarioA,
            new DateTime(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc),
            ReservationStatus.Pending, new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc)));
        readStore.Agregar(NuevaReserva(
            Guid.NewGuid(), Guid.NewGuid(), UsuarioB,
            new DateTime(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc),
            ReservationStatus.Pending, new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc)));

        var currentUser = new MutableCurrentUser(UsuarioA, "Standard");
        var handler = new ListMyReservationsQueryHandler(readStore, currentUser);

        // Act
        var resultado = await handler.Handle(new ListMyReservationsQuery("renter", 1, 20), CancellationToken.None);

        // Assert
        Assert.All(resultado.Items, r => Assert.Equal(UsuarioA, r.RenterId));
        Assert.Single(resultado.Items);
    }

    [Fact]
    public async Task RF_NEG_02_F6_ListAllReservations_filtra_por_estado()
    {
        // Arrange
        var readStore = new InMemoryReservationReadStore();
        readStore.Agregar(NuevaReserva(
            Guid.NewGuid(), Guid.NewGuid(), UsuarioA,
            new DateTime(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc),
            ReservationStatus.Confirmed, new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc)));
        readStore.Agregar(NuevaReserva(
            Guid.NewGuid(), Guid.NewGuid(), UsuarioB,
            new DateTime(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc),
            ReservationStatus.Pending, new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc)));

        var handler = new ListAllReservationsQueryHandler(readStore);

        // Act
        var resultado = await handler.Handle(new ListAllReservationsQuery(ReservationStatus.Confirmed, 1, 20), CancellationToken.None);

        // Assert
        Assert.All(resultado.Items, r => Assert.Equal(ReservationStatus.Confirmed, r.Status));
        Assert.Single(resultado.Items);
    }
}

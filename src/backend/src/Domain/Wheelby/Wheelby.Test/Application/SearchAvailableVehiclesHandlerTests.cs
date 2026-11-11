using Shared.Application.Abstractions;
using Wheelby.Application.Features.SearchAvailableVehicles;
using Wheelby.Application.Abstractions;
using Wheelby.Domain.Reservations;
using Wheelby.Test.TestSupport;

// F2: Buscar vehículos disponibles.
public class SearchAvailableVehiclesHandlerTests
{
    private static readonly DateTime Desde = new(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Hasta = new(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc);

    private static SearchAvailableVehiclesQueryHandler CrearHandler(
        out InMemoryVehicleReadStore readStore,
        out MutableCurrentUser currentUser)
    {
        readStore = new InMemoryVehicleReadStore();
        currentUser = new MutableCurrentUser(Guid.NewGuid(), "Standard");
        var clock = new FixedClock(new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc));
        return new SearchAvailableVehiclesQueryHandler(readStore, currentUser, new FakeExchangeRateProvider());
    }

    [Fact]
    public async Task RF_NEG_02_F2_excluye_vehiculos_del_propio_usuario()
    {
        // Arrange
        var handler = CrearHandler(out var readStore, out var currentUser);
        var otroUsuario = Guid.NewGuid();

        readStore.AgregarVehiculo(Guid.NewGuid(), currentUser.Id, "ABC-123"); // propio
        readStore.AgregarVehiculo(Guid.NewGuid(), otroUsuario, "XYZ-789"); // ajeno

        // Act
        var resultado = await handler.Handle(
            new SearchAvailableVehiclesQuery(Desde, Hasta, 1, 20),
            CancellationToken.None);

        // Assert
        Assert.Single(resultado.Items);
        Assert.Equal(otroUsuario, resultado.Items[0].OwnerId);
    }

    [Fact]
    public async Task RF_NEG_02_F2_excluye_vehiculos_con_reserva_Confirmed_que_solapa()
    {
        // Arrange
        var handler = CrearHandler(out var readStore, out var currentUser);
        var vehiculoAjeno = Guid.NewGuid();

        readStore.AgregarVehiculo(vehiculoAjeno, Guid.NewGuid(), "ABC-123");
        // Reserva Confirmed que solapa el rango buscado
        readStore.AgregarReserva(vehiculoAjeno, ReservationStatus.Confirmed,
            new DateTime(2026, 11, 6, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 8, 0, 0, 0, DateTimeKind.Utc));

        // Act
        var resultado = await handler.Handle(
            new SearchAvailableVehiclesQuery(Desde, Hasta, 1, 20),
            CancellationToken.None);

        // Assert
        Assert.Empty(resultado.Items);
    }

    [Fact]
    public async Task RF_NEG_02_F2_excluye_vehiculos_con_reserva_InProgress_que_solapa()
    {
        // Arrange
        var handler = CrearHandler(out var readStore, out var currentUser);
        var vehiculoAjeno = Guid.NewGuid();

        readStore.AgregarVehiculo(vehiculoAjeno, Guid.NewGuid(), "ABC-123");
        readStore.AgregarReserva(vehiculoAjeno, ReservationStatus.InProgress,
            new DateTime(2026, 11, 6, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 8, 0, 0, 0, DateTimeKind.Utc));

        // Act
        var resultado = await handler.Handle(
            new SearchAvailableVehiclesQuery(Desde, Hasta, 1, 20),
            CancellationToken.None);

        // Assert
        Assert.Empty(resultado.Items);
    }

    [Fact]
    public async Task RF_NEG_02_F2_no_excluye_por_reserva_Pending()
    {
        // Arrange
        var handler = CrearHandler(out var readStore, out var currentUser);
        var vehiculoAjeno = Guid.NewGuid();

        readStore.AgregarVehiculo(vehiculoAjeno, Guid.NewGuid(), "ABC-123");
        // Reserva Pending no bloquea el calendario
        readStore.AgregarReserva(vehiculoAjeno, ReservationStatus.Pending,
            new DateTime(2026, 11, 6, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 8, 0, 0, 0, DateTimeKind.Utc));

        // Act
        var resultado = await handler.Handle(
            new SearchAvailableVehiclesQuery(Desde, Hasta, 1, 20),
            CancellationToken.None);

        // Assert
        Assert.Single(resultado.Items);
    }

    [Fact]
    public void RF_NEG_02_F2_rango_invalido_from_igual_a_to_se_rechaza()
    {
        // Arrange
        var validator = new SearchAvailableVehiclesQueryValidator();
        var query = new SearchAvailableVehiclesQuery(Desde, Desde, 1, 20);

        // Act
        var resultado = validator.Validate(query);

        // Assert
        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName.Contains("To") || e.ErrorMessage.Contains("posterior"));
    }
}

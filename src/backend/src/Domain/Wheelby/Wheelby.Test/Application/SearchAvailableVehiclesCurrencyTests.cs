using SharedKernel.Exceptions;
using Wheelby.Application.Features.SearchAvailableVehicles;
using Wheelby.Test.TestSupport;

// RD-10: la búsqueda convierte el precio solo cuando se pide otra moneda.
public class SearchAvailableVehiclesCurrencyTests
{
    private static readonly DateTime Desde = new(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Hasta = new(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc);

    private static SearchAvailableVehiclesQueryHandler CrearHandler(FakeExchangeRateProvider proveedor, int vehiculos = 2)
    {
        var readStore = new InMemoryVehicleReadStore();
        for (var i = 0; i < vehiculos; i++)
            readStore.AgregarVehiculo(Guid.NewGuid(), Guid.NewGuid(), $"ABC-{i:000}"); // precio fijo 100 DOP
        return new SearchAvailableVehiclesQueryHandler(readStore, new MutableCurrentUser(Guid.NewGuid()), proveedor);
    }

    [Fact]
    public async Task RD_10_Sin_currency_no_llama_al_proveedor()
    {
        // Arrange
        var proveedor = new FakeExchangeRateProvider(0.0168m);
        var handler = CrearHandler(proveedor);

        // Act
        var resultado = await handler.Handle(new SearchAvailableVehiclesQuery(Desde, Hasta), CancellationToken.None);

        // Assert
        Assert.Equal(0, proveedor.Calls);
        Assert.All(resultado.Items, v =>
        {
            Assert.Equal("DOP", v.Currency);
            Assert.Null(v.ConvertedPricePerDay);
            Assert.Null(v.ConvertedCurrency);
            Assert.Null(v.ExchangeRate);
        });
    }

    [Fact]
    public async Task RD_10_Con_DOP_no_llama_al_proveedor()
    {
        // Arrange
        var proveedor = new FakeExchangeRateProvider(0.0168m);
        var handler = CrearHandler(proveedor);

        // Act
        await handler.Handle(new SearchAvailableVehiclesQuery(Desde, Hasta, Currency: "DOP"), CancellationToken.None);

        // Assert
        Assert.Equal(0, proveedor.Calls);
    }

    [Fact]
    public async Task RD_10_Con_USD_convierte_con_la_tasa_y_redondea_a_2_decimales()
    {
        // Arrange
        var proveedor = new FakeExchangeRateProvider(0.016789m);
        var handler = CrearHandler(proveedor, vehiculos: 1);

        // Act
        var resultado = await handler.Handle(new SearchAvailableVehiclesQuery(Desde, Hasta, Currency: "USD"), CancellationToken.None);

        // Assert
        var v = Assert.Single(resultado.Items);
        Assert.Equal(100m, v.PricePerDay);
        Assert.Equal("DOP", v.Currency);
        Assert.Equal(1.68m, v.ConvertedPricePerDay); // 100 * 0.016789 = 1.6789
        Assert.Equal("USD", v.ConvertedCurrency);
        Assert.Equal(0.016789m, v.ExchangeRate);
        Assert.Equal(("DOP", "USD"), proveedor.Requests.Single());
    }

    [Fact]
    public async Task RD_10_Con_USD_llama_una_sola_vez_aunque_haya_varios_vehiculos()
    {
        // Arrange
        var proveedor = new FakeExchangeRateProvider(0.0168m);
        var handler = CrearHandler(proveedor, vehiculos: 3);

        // Act
        var resultado = await handler.Handle(new SearchAvailableVehiclesQuery(Desde, Hasta, Currency: "USD"), CancellationToken.None);

        // Assert
        Assert.Equal(3, resultado.Items.Count);
        Assert.Equal(1, proveedor.Calls);
    }

    [Fact]
    public async Task RD_08_Si_el_proveedor_lanza_503_la_excepcion_se_propaga()
    {
        // Arrange
        var proveedor = new FakeExchangeRateProvider(failWith: new ServiceUnavailableException("caido"));
        var handler = CrearHandler(proveedor);

        // Act
        var ex = await Record.ExceptionAsync(() =>
            handler.Handle(new SearchAvailableVehiclesQuery(Desde, Hasta, Currency: "USD"), CancellationToken.None));

        // Assert
        var unavailable = Assert.IsType<ServiceUnavailableException>(ex);
        Assert.Equal(503, unavailable.StatusCode);
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDX")]
    [InlineData("12$")]
    [InlineData("usd")]
    public void RD_07_Validador_rechaza_monedas_que_no_son_3_letras(string currency)
    {
        // Arrange
        var validator = new SearchAvailableVehiclesQueryValidator();

        // Act
        var result = validator.Validate(new SearchAvailableVehiclesQuery(Desde, Hasta, Currency: currency));

        // Assert
        Assert.Contains(result.Errors, e => e.PropertyName == "Currency");
    }

    [Theory]
    [InlineData("USD")]
    [InlineData(null)]
    public void RD_07_Validador_acepta_USD_y_null(string? currency)
    {
        // Arrange
        var validator = new SearchAvailableVehiclesQueryValidator();

        // Act
        var result = validator.Validate(new SearchAvailableVehiclesQuery(Desde, Hasta, Currency: currency));

        // Assert
        Assert.True(result.IsValid);
    }
}

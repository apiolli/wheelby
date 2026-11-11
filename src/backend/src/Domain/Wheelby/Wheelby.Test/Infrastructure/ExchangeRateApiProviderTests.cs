using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SharedKernel.Exceptions;
using Wheelby.Infrastructure.ExchangeRates;
using Wheelby.Test.TestSupport;

// RD-08 y RD-10: ExchangeRateApiProvider con un HttpMessageHandler falso, sin red.
public class ExchangeRateApiProviderTests
{
    private const string Clave = "clave-secreta-de-prueba-123";

    private const string Exito =
        """{"result":"success","base_code":"DOP","target_code":"USD","conversion_rate":0.0168}""";

    private static ExchangeRateApiProvider Crear(StubHttpMessageHandler stub, string? apiKey = Clave, IMemoryCache? cache = null)
    {
        var http = new HttpClient(stub) { BaseAddress = new Uri("https://v6.exchangerate-api.com/v6/") };
        var options = Options.Create(new ExchangeRateOptions { ApiKey = apiKey, TimeoutSeconds = 5, CacheMinutes = 60 });
        return new ExchangeRateApiProvider(
            http, options, cache ?? new MemoryCache(new MemoryCacheOptions()),
            NullLogger<ExchangeRateApiProvider>.Instance);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task RD_10_Sin_clave_responde_503_sin_hacer_peticiones(string? apiKey)
    {
        // Arrange
        var stub = StubHttpMessageHandler.Json(HttpStatusCode.OK, Exito);
        var proveedor = Crear(stub, apiKey);

        // Act
        var ex = await Record.ExceptionAsync(() => proveedor.GetRateAsync("DOP", "USD", CancellationToken.None));

        // Assert
        var unavailable = Assert.IsType<ServiceUnavailableException>(ex);
        Assert.Equal("La conversión de moneda no está disponible: falta configurar ExchangeRate__ApiKey.", unavailable.Message);
        Assert.Empty(stub.Requests);
    }

    [Fact]
    public async Task RD_10_Exito_devuelve_la_tasa()
    {
        // Arrange
        var stub = StubHttpMessageHandler.Json(HttpStatusCode.OK, Exito);
        var proveedor = Crear(stub);

        // Act
        var rate = await proveedor.GetRateAsync("DOP", "USD", CancellationToken.None);

        // Assert
        Assert.Equal(0.0168m, rate);
    }

    [Fact]
    public async Task RD_10_La_clave_va_en_la_cabecera_y_no_en_la_URL()
    {
        // Arrange
        var stub = StubHttpMessageHandler.Json(HttpStatusCode.OK, Exito);
        var proveedor = Crear(stub);

        // Act
        await proveedor.GetRateAsync("DOP", "USD", CancellationToken.None);

        // Assert
        var request = Assert.Single(stub.Requests);
        Assert.Equal("https://v6.exchangerate-api.com/v6/pair/DOP/USD", request.RequestUri!.ToString());
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
        Assert.Equal(Clave, request.Headers.Authorization.Parameter);
    }

    [Theory]
    [InlineData("invalid-key", HttpStatusCode.Forbidden)]
    [InlineData("inactive-account", HttpStatusCode.Forbidden)]
    public async Task RD_08_Clave_rechazada_responde_503(string errorType, HttpStatusCode status)
    {
        // Arrange
        var stub = StubHttpMessageHandler.Json(status, $$"""{"result":"error","error-type":"{{errorType}}"}""");
        var proveedor = Crear(stub);

        // Act
        var ex = await Record.ExceptionAsync(() => proveedor.GetRateAsync("DOP", "USD", CancellationToken.None));

        // Assert
        var unavailable = Assert.IsType<ServiceUnavailableException>(ex);
        Assert.Equal("El servicio de tasas de cambio rechazó la clave configurada.", unavailable.Message);
    }

    [Fact]
    public async Task RD_08_Cuota_agotada_responde_503()
    {
        // Arrange
        var stub = StubHttpMessageHandler.Json(HttpStatusCode.TooManyRequests, """{"result":"error","error-type":"quota-reached"}""");
        var proveedor = Crear(stub);

        // Act
        var ex = await Record.ExceptionAsync(() => proveedor.GetRateAsync("DOP", "USD", CancellationToken.None));

        // Assert
        Assert.IsType<ServiceUnavailableException>(ex);
    }

    [Fact]
    public async Task RD_08_Moneda_no_soportada_responde_400()
    {
        // Arrange
        var stub = StubHttpMessageHandler.Json(HttpStatusCode.NotFound, """{"result":"error","error-type":"unsupported-code"}""");
        var proveedor = Crear(stub);

        // Act
        var ex = await Record.ExceptionAsync(() => proveedor.GetRateAsync("DOP", "XYZ", CancellationToken.None));

        // Assert
        var badRequest = Assert.IsType<BadRequestException>(ex);
        Assert.Equal(400, badRequest.StatusCode);
        Assert.Equal("La moneda XYZ no está soportada.", badRequest.Message);
    }

    [Fact]
    public async Task RD_08_Timeout_responde_503()
    {
        // Arrange (HttpClient señala su timeout con TaskCanceledException)
        var stub = StubHttpMessageHandler.Throws(new TaskCanceledException("timeout"));
        var proveedor = Crear(stub);

        // Act
        var ex = await Record.ExceptionAsync(() => proveedor.GetRateAsync("DOP", "USD", CancellationToken.None));

        // Assert
        var unavailable = Assert.IsType<ServiceUnavailableException>(ex);
        Assert.Equal("El servicio de tasas de cambio no respondió. Intenta más tarde.", unavailable.Message);
    }

    [Fact]
    public async Task RD_08_Error_de_red_responde_503()
    {
        // Arrange
        var stub = StubHttpMessageHandler.Throws(new HttpRequestException("sin red"));
        var proveedor = Crear(stub);

        // Act
        var ex = await Record.ExceptionAsync(() => proveedor.GetRateAsync("DOP", "USD", CancellationToken.None));

        // Assert
        Assert.IsType<ServiceUnavailableException>(ex);
    }

    [Fact]
    public async Task RD_08_JSON_invalido_responde_503()
    {
        // Arrange
        var stub = StubHttpMessageHandler.Json(HttpStatusCode.BadGateway, "<html>502</html>");
        var proveedor = Crear(stub);

        // Act
        var ex = await Record.ExceptionAsync(() => proveedor.GetRateAsync("DOP", "USD", CancellationToken.None));

        // Assert
        Assert.IsType<ServiceUnavailableException>(ex);
    }

    [Fact]
    public async Task RD_10_Un_segundo_pedido_del_mismo_par_usa_la_cache()
    {
        // Arrange
        var stub = StubHttpMessageHandler.Json(HttpStatusCode.OK, Exito);
        var proveedor = Crear(stub);
        await proveedor.GetRateAsync("DOP", "USD", CancellationToken.None);

        // Act
        var rate = await proveedor.GetRateAsync("DOP", "USD", CancellationToken.None);

        // Assert
        Assert.Equal(0.0168m, rate);
        Assert.Single(stub.Requests);
    }

    [Fact]
    public async Task RD_10_La_clave_no_aparece_en_ningun_mensaje_de_excepcion()
    {
        // Arrange
        var casos = new[]
        {
            StubHttpMessageHandler.Json(HttpStatusCode.Forbidden, """{"result":"error","error-type":"invalid-key"}"""),
            StubHttpMessageHandler.Json(HttpStatusCode.TooManyRequests, """{"result":"error","error-type":"quota-reached"}"""),
            StubHttpMessageHandler.Json(HttpStatusCode.NotFound, """{"result":"error","error-type":"unsupported-code"}"""),
            StubHttpMessageHandler.Json(HttpStatusCode.OK, "no es json"),
            StubHttpMessageHandler.Throws(new TaskCanceledException()),
            StubHttpMessageHandler.Throws(new HttpRequestException()),
        };

        // Act
        var mensajes = new List<string>();
        foreach (var stub in casos)
        {
            var ex = await Record.ExceptionAsync(() => Crear(stub).GetRateAsync("DOP", "USD", CancellationToken.None));
            mensajes.Add(ex!.ToString());
        }

        // Assert
        Assert.Equal(6, mensajes.Count);
        Assert.All(mensajes, m => Assert.DoesNotContain(Clave, m));
    }
}

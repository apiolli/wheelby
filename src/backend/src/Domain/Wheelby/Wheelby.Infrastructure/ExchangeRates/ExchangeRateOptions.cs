namespace Wheelby.Infrastructure.ExchangeRates;

// Configuración de ExchangeRate-API (sección "ExchangeRate").
// La API no exige ApiKey al arrancar: sin clave, solo la conversión responde 503.
internal sealed class ExchangeRateOptions
{
    public const string SectionName = "ExchangeRate";

    public string? ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://v6.exchangerate-api.com/v6/";
    public int TimeoutSeconds { get; set; } = 5;
    public int CacheMinutes { get; set; } = 60;
}

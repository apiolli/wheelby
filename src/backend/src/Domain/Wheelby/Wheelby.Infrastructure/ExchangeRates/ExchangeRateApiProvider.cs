using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel.Exceptions;
using Wheelby.Application.Abstractions;

namespace Wheelby.Infrastructure.ExchangeRates;

// RD-10: ExchangeRate-API, endpoint de par (GET pair/{from}/{to}).
// La clave viaja en la cabecera Authorization: Bearer, nunca en la URL, en un
// mensaje de excepción ni en el log.
internal sealed class ExchangeRateApiProvider(
    HttpClient http,
    IOptions<ExchangeRateOptions> options,
    IMemoryCache cache,
    ILogger<ExchangeRateApiProvider> logger) : IExchangeRateProvider
{
    public const string MissingKeyMessage =
        "La conversión de moneda no está disponible: falta configurar ExchangeRate__ApiKey.";
    public const string NoResponseMessage =
        "El servicio de tasas de cambio no respondió. Intenta más tarde.";
    public const string RejectedKeyMessage =
        "El servicio de tasas de cambio rechazó la clave configurada.";
    public const string QuotaReachedMessage =
        "Se agotó la cuota del servicio de tasas de cambio. Intenta más tarde.";

    private readonly ExchangeRateOptions _options = options.Value;

    public async Task<decimal> GetRateAsync(
        string fromCurrency, string toCurrency, CancellationToken cancellationToken)
    {
        // La cuota del plan gratis es de 1500 llamadas al mes: cada par se guarda en caché.
        var cacheKey = $"exchange-rate:{fromCurrency}:{toCurrency}";
        if (cache.TryGetValue(cacheKey, out decimal cached))
            return cached;

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new ServiceUnavailableException(MissingKeyMessage);

        var (statusCode, body) = await SendAsync(fromCurrency, toCurrency, cancellationToken);
        var rate = ParseRate(statusCode, body, toCurrency);

        cache.Set(cacheKey, rate, TimeSpan.FromMinutes(Math.Max(1, _options.CacheMinutes)));
        return rate;
    }

    private async Task<(int StatusCode, string Body)> SendAsync(
        string fromCurrency, string toCurrency, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds)));

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"pair/{Uri.EscapeDataString(fromCurrency)}/{Uri.EscapeDataString(toCurrency)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        try
        {
            using var response = await http.SendAsync(request, timeout.Token);
            // El cuerpo se lee aunque el estado no sea 200: el proveedor trae ahí el error-type.
            var body = await response.Content.ReadAsStringAsync(timeout.Token);
            return ((int)response.StatusCode, body);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Exchange rate request timed out ({From} -> {To}).", fromCurrency, toCurrency);
            throw new ServiceUnavailableException(NoResponseMessage);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning("Exchange rate request failed ({From} -> {To}): {Error}.",
                fromCurrency, toCurrency, ex.HttpRequestError);
            throw new ServiceUnavailableException(NoResponseMessage);
        }
    }

    private decimal ParseRate(int statusCode, string body, string toCurrency)
    {
        string? result;
        string? errorType = null;
        decimal? rate = null;

        try
        {
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            result = root.TryGetProperty("result", out var r) ? r.GetString() : null;

            if (root.TryGetProperty("error-type", out var e))
                errorType = e.GetString();

            if (root.TryGetProperty("conversion_rate", out var c) && c.ValueKind == JsonValueKind.Number)
                rate = c.GetDecimal();
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException)
        {
            logger.LogWarning("Exchange rate response is not valid JSON (HTTP {Status}).", statusCode);
            throw new ServiceUnavailableException(NoResponseMessage);
        }

        if (result == "success" && rate is > 0)
            return rate.Value;

        logger.LogWarning("Exchange rate provider returned an error (HTTP {Status}): {ErrorType}.",
            statusCode, errorType ?? "none");

        throw errorType switch
        {
            "invalid-key" or "inactive-account" => new ServiceUnavailableException(RejectedKeyMessage),
            "unsupported-code" or "unknown-code" => new BadRequestException(
                string.Create(CultureInfo.InvariantCulture, $"La moneda {toCurrency} no está soportada.")),
            "quota-reached" => new ServiceUnavailableException(QuotaReachedMessage),
            _ => new ServiceUnavailableException(NoResponseMessage)
        };
    }
}

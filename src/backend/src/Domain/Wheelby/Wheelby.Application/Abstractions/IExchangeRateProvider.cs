namespace Wheelby.Application.Abstractions;

// RD-10: tasa de cambio de un servicio externo. Cualquier falla del proveedor
// se traduce en ServiceUnavailableException (503); una moneda no soportada, en 400.
public interface IExchangeRateProvider
{
    Task<decimal> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken);
}

using Wheelby.Application.Abstractions;

namespace Wheelby.Test.TestSupport;

// Doble del proveedor de tasas: devuelve una tasa fija o lanza la excepción indicada,
// y cuenta las llamadas.
public sealed class FakeExchangeRateProvider(decimal rate = 1m, Exception? failWith = null) : IExchangeRateProvider
{
    public int Calls { get; private set; }
    public List<(string From, string To)> Requests { get; } = new();

    public Task<decimal> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken)
    {
        Calls++;
        Requests.Add((fromCurrency, toCurrency));
        if (failWith is not null)
            throw failWith;
        return Task.FromResult(rate);
    }
}

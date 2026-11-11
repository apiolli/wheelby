using AccessControl.Contracts;
using MediatR;
using Wheelby.Application.Abstractions;
using Wheelby.Application.Common;
using Wheelby.Application.DTOs;

namespace Wheelby.Application.Features.SearchAvailableVehicles;

internal sealed class SearchAvailableVehiclesQueryHandler(
    IVehicleReadStore vehicleReadStore,
    ICurrentUser currentUser,
    IExchangeRateProvider exchangeRates)
    : IRequestHandler<SearchAvailableVehiclesQuery, PagedResult<VehicleSummaryDTO>>
{
    public const string BaseCurrency = "DOP";

    public async Task<PagedResult<VehicleSummaryDTO>> Handle(
        SearchAvailableVehiclesQuery query,
        CancellationToken cancellationToken)
    {
        var page = await vehicleReadStore.SearchAvailableAsync(
            query.From,
            query.To,
            currentUser.Id,
            query.Page,
            query.PageSize,
            cancellationToken);

        // Sin moneda, o en DOP, la búsqueda nunca depende del servicio externo.
        if (query.Currency is null || query.Currency == BaseCurrency)
            return page;

        // Una sola consulta de la tasa para toda la página. Si el proveedor falla,
        // la ServiceUnavailableException sube y la respuesta es 503.
        var rate = await exchangeRates.GetRateAsync(BaseCurrency, query.Currency, cancellationToken);

        var converted = page.Items
            .Select(v => v with
            {
                ConvertedPricePerDay = Math.Round(v.PricePerDay * rate, 2),
                ConvertedCurrency = query.Currency,
                ExchangeRate = rate
            })
            .ToList();

        return page with { Items = converted };
    }
}

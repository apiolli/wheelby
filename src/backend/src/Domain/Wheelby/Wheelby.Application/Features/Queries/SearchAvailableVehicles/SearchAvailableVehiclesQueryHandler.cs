using AccessControl.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedKernel.Exceptions;
using Wheelby.Application.Abstractions;
using Wheelby.Application.Common;
using Wheelby.Application.DTOs;

namespace Wheelby.Application.Features.SearchAvailableVehicles;

internal sealed class SearchAvailableVehiclesQueryHandler(
    IVehicleReadStore vehicleReadStore,
    ICurrentUser currentUser,
    IExchangeRateProvider exchangeRates,
    ILogger<SearchAvailableVehiclesQueryHandler> logger)
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

        // Una sola consulta de la tasa para toda la página (el proveedor la cachea).
        // Si el servicio externo no está disponible, la búsqueda no falla: responde
        // en DOP con los campos de conversión en null. Una moneda no soportada
        // sigue siendo un 400.
        decimal rate;
        try
        {
            rate = await exchangeRates.GetRateAsync(BaseCurrency, query.Currency, cancellationToken);
        }
        catch (ServiceUnavailableException ex)
        {
            logger.LogWarning("Exchange rate unavailable, answering in {Currency}: {Reason}", BaseCurrency, ex.Message);
            return page;
        }

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

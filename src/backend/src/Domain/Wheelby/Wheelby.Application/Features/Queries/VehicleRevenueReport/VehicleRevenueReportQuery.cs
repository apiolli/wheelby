using AccessControl.Contracts;
using FluentValidation;
using MediatR;
using Shared.Application.Abstractions;
using Shared.Application.Reports;
using Wheelby.Application.Abstractions;

namespace Wheelby.Application.Features.VehicleRevenueReport;

// RF-REP-02: GET /reports/vehicle-revenue?from=&to=.
// ¿Cuánto generó cada vehículo y cuántos días estuvo alquilado en un período?
public sealed record VehicleRevenueReportQuery(string? From, string? To) : IQuery<VehicleRevenueReportDTO>;

public sealed record VehicleRevenueRow(
    Guid VehicleId,
    string Brand,
    string Model,
    string Plate,
    int Reservations,
    int Days,
    decimal TotalAmount,
    decimal AverageAmount);

public sealed record VehicleRevenueTotals(int Reservations, int Days, decimal TotalAmount);

// Montos en DOP, los congelados en cada reserva. scope: "all" (Administrador) u "own".
public sealed record VehicleRevenueReportDTO(
    DateOnly From,
    DateOnly To,
    string Scope,
    string Currency,
    VehicleRevenueTotals Totals,
    IReadOnlyList<VehicleRevenueRow> Rows);

internal sealed class VehicleRevenueReportQueryValidator : AbstractValidator<VehicleRevenueReportQuery>
{
    public VehicleRevenueReportQueryValidator() => this.AddReportRangeRules(x => x.From, x => x.To);
}

internal sealed class VehicleRevenueReportQueryHandler(
    IVehicleRevenueReportStore reports,
    ICurrentUser currentUser) : IRequestHandler<VehicleRevenueReportQuery, VehicleRevenueReportDTO>
{
    public const string Currency = "DOP";

    public async Task<VehicleRevenueReportDTO> Handle(VehicleRevenueReportQuery request, CancellationToken cancellationToken)
    {
        var range = ReportDateRange.Parse(request.From, request.To);

        // RF-REP-04: el Estándar solo ve sus vehículos (un inquilino sin vehículos recibe 0 filas).
        // El Administrador ve la plataforma completa aunque no entre a /vehicles ni a /reservations.
        var isAdministrator = currentUser.Role == AccessControlRoles.Administrator;
        Guid? ownerId = isAdministrator ? null : currentUser.Id;

        var groups = await reports.SumByVehicleAsync(range.StartUtc, range.EndUtc, ownerId, cancellationToken);

        var rows = groups
            .Select(g => new VehicleRevenueRow(
                g.VehicleId, g.Brand, g.Model, g.Plate, g.Reservations, g.Days, g.TotalAmount,
                g.Reservations == 0 ? 0m : decimal.Round(g.TotalAmount / g.Reservations, 2, MidpointRounding.AwayFromZero)))
            .OrderByDescending(r => r.TotalAmount)
            .ThenBy(r => r.Plate, StringComparer.Ordinal)
            .ToList();

        var totals = new VehicleRevenueTotals(
            rows.Sum(r => r.Reservations), rows.Sum(r => r.Days), rows.Sum(r => r.TotalAmount));

        return new VehicleRevenueReportDTO(range.From, range.To, isAdministrator ? "all" : "own", Currency, totals, rows);
    }
}

namespace Wheelby.Application.Abstractions;

// RF-REP-02 y 03: la agregación la hace PostgreSQL (GROUP BY con COUNT y SUM).
public interface IVehicleRevenueReportStore
{
    // Reservas Confirmed, InProgress o Completed con StartDate en [startUtc, endUtc),
    // agrupadas por vehículo. ownerId null = toda la plataforma; con valor = sus vehículos.
    Task<IReadOnlyList<VehicleRevenueGroup>> SumByVehicleAsync(
        DateTime startUtc, DateTime endUtc, Guid? ownerId, CancellationToken cancellationToken);
}

public sealed record VehicleRevenueGroup(
    Guid VehicleId, string Brand, string Model, string Plate, int Reservations, int Days, decimal TotalAmount);

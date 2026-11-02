using Microsoft.EntityFrameworkCore;
using Wheelby.Application.Abstractions;
using Wheelby.Domain.Vehicles;

namespace Wheelby.Infrastructure.Persistence;

internal sealed class VehicleRepository(WheelbyDbContext db) : IVehicleRepository
{
    public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => db.Vehicles.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public Task<bool> PlateExistsAsync(string plate, CancellationToken cancellationToken)
    {
        var normalized = plate.Trim().ToUpperInvariant();
        return db.Vehicles.AnyAsync(v => v.Plate == normalized, cancellationToken);
    }

    public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
        => db.Vehicles.AddAsync(vehicle, cancellationToken).AsTask();
}

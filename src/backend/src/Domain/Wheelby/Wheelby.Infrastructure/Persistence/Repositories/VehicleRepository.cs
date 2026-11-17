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

    public Task<bool> PlateExistsAsync(string plate, Guid excludeVehicleId, CancellationToken cancellationToken)
    {
        var normalized = plate.Trim().ToUpperInvariant();
        return db.Vehicles.AnyAsync(v => v.Plate == normalized && v.Id != excludeVehicleId, cancellationToken);
    }

    public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
        => db.Vehicles.AddAsync(vehicle, cancellationToken).AsTask();

    public Task<int> CountPhotosAsync(Guid vehicleId, CancellationToken cancellationToken)
        => db.VehiclePhotos.CountAsync(p => p.VehicleId == vehicleId, cancellationToken);

    public Task<VehiclePhoto?> GetPhotoAsync(Guid vehicleId, Guid documentId, CancellationToken cancellationToken)
        => db.VehiclePhotos.FirstOrDefaultAsync(p => p.VehicleId == vehicleId && p.DocumentId == documentId, cancellationToken);

    public Task AddPhotoAsync(VehiclePhoto photo, CancellationToken cancellationToken)
        => db.VehiclePhotos.AddAsync(photo, cancellationToken).AsTask();

    public void RemovePhoto(VehiclePhoto photo) => db.VehiclePhotos.Remove(photo);
}

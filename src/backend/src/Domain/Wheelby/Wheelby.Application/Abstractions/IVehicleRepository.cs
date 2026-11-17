using Wheelby.Domain.Vehicles;

namespace Wheelby.Application.Abstractions;

// Puerto de escritura del agregado Vehicle y de sus fotos.
public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> PlateExistsAsync(string plate, CancellationToken cancellationToken = default);
    Task<bool> PlateExistsAsync(string plate, Guid excludeVehicleId, CancellationToken cancellationToken = default);
    Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default);

    Task<int> CountPhotosAsync(Guid vehicleId, CancellationToken cancellationToken = default);
    Task<VehiclePhoto?> GetPhotoAsync(Guid vehicleId, Guid documentId, CancellationToken cancellationToken = default);
    Task AddPhotoAsync(VehiclePhoto photo, CancellationToken cancellationToken = default);
    void RemovePhoto(VehiclePhoto photo);
}

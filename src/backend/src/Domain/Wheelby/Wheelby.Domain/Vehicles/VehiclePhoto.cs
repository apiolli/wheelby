using SharedKernel.Domain;

namespace Wheelby.Domain.Vehicles;

// Foto de un vehículo. RD-03: Wheelby guarda solo el DocumentId; el archivo vive
// en Documents y se sirve por /vehicles/{id}/photos/{documentId}.
public sealed class VehiclePhoto : Entity<Guid>
{
    public const int MaxPerVehicle = 10;

    public Guid VehicleId { get; private set; }
    public Guid DocumentId { get; private set; }
    public DateTime AddedAt { get; private set; }

    private VehiclePhoto() { }

    public VehiclePhoto(Guid vehicleId, Guid documentId, DateTime utcNow) : base(Guid.NewGuid())
    {
        VehicleId = vehicleId;
        DocumentId = documentId;
        AddedAt = utcNow;
    }
}

using Shared.Application.Abstractions;

namespace Wheelby.Application.Features.RemoveVehiclePhoto;

// Desvincula la foto. El documento sigue en Documents; su dueño lo borra allí si quiere.
public sealed record RemoveVehiclePhotoCommand(Guid VehicleId, Guid DocumentId) : ICommand;

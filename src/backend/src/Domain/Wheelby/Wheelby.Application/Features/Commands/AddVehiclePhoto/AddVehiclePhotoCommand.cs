using Shared.Application.Abstractions;

namespace Wheelby.Application.Features.AddVehiclePhoto;

// La imagen se sube antes por POST /documents; aquí solo se vincula su id.
public sealed record AddVehiclePhotoCommand(Guid VehicleId, Guid DocumentId) : ICommand;

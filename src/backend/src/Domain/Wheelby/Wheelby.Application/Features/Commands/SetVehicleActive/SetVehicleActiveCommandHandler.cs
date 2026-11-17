using AccessControl.Contracts;
using MediatR;
using Shared.Application.Abstractions;
using Wheelby.Application.Abstractions;
using Wheelby.Application.Common;

namespace Wheelby.Application.Features.SetVehicleActive;

// Desactivar saca el vehículo de la búsqueda y bloquea reservas nuevas. No se
// permite con reservas Confirmed o InProgress. Las solicitudes Pending quedan:
// el anfitrión las rechaza o expiran solas.
internal sealed class SetVehicleActiveCommandHandler(
    IVehicleRepository vehicles,
    IReservationRepository reservations,
    IWheelbyUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<SetVehicleActiveCommand, Unit>
{
    public async Task<Unit> Handle(SetVehicleActiveCommand command, CancellationToken cancellationToken)
    {
        var vehicle = await VehicleOwnership.GetOwnedAsync(vehicles, currentUser, command.Id, cancellationToken);

        if (command.IsActive)
        {
            vehicle.Activate(clock.UtcNow);
        }
        else
        {
            var hasBlocking = await reservations.HasBlockingReservationsAsync(vehicle.Id, cancellationToken);
            vehicle.Deactivate(hasBlocking, clock.UtcNow);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

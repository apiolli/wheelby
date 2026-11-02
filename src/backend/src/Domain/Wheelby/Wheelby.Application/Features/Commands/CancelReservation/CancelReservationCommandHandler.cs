using AccessControl.Contracts;
using MediatR;
using Shared.Application.Abstractions;
using SharedKernel.Exceptions;
using Wheelby.Application.Abstractions;
using Wheelby.Domain.Reservations;
using Wheelby.Domain.Vehicles;

namespace Wheelby.Application.Features.CancelReservation;

internal sealed class CancelReservationCommandHandler(
    IVehicleRepository vehicles,
    IReservationRepository reservations,
    IWheelbyUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<CancelReservationCommand, Unit>
{
    public async Task<Unit> Handle(
        CancelReservationCommand command,
        CancellationToken cancellationToken)
    {
        var reservation = await reservations.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("La reserva no existe.");

        var vehicle = await vehicles.GetByIdAsync(reservation.VehicleId, cancellationToken)
            ?? throw new NotFoundException("El vehículo de la reserva no existe.");

        // El actor es Renter si el usuario es el inquilino, Host si es el dueño;
        // si no participa, 403.
        var isRenter = reservation.RenterId == currentUser.Id;
        var isHost = vehicle.OwnerId == currentUser.Id;

        if (!isRenter && !isHost)
            throw new ForbiddenException("No participas en esta reserva, no puedes cancelarla.");

        var actor = isRenter ? ReservationActor.Renter : ReservationActor.Host;

        reservation.TransitionTo(ReservationStatus.Cancelled, actor, clock.UtcNow, command.Reason);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

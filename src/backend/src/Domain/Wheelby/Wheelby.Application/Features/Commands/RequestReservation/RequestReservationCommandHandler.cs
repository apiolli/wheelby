using AccessControl.Contracts;
using MediatR;
using Shared.Application.Abstractions;
using SharedKernel.Exceptions;
using Wheelby.Domain.Errors;
using Wheelby.Application.Abstractions;
using Wheelby.Domain.Reservations;
using Wheelby.Domain.Vehicles;

namespace Wheelby.Application.Features.RequestReservation;

internal sealed class RequestReservationCommandHandler(
    IVehicleRepository vehicles,
    IReservationRepository reservations,
    IWheelbyUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<RequestReservationCommand, Guid>
{
    public async Task<Guid> Handle(
        RequestReservationCommand command,
        CancellationToken cancellationToken)
    {
        var vehicle = await vehicles.GetByIdAsync(command.VehicleId, cancellationToken)
            ?? throw new NotFoundException("El vehículo no existe.", WheelbyErrorCodes.VehicleNotFound);

        // No se puede reservar el propio vehículo.
        if (vehicle.OwnerId == currentUser.Id)
            throw new ForbiddenException("No puedes reservar tu propio vehículo.", WheelbyErrorCodes.ReservationOwnVehicle);

        // Solo Confirmed e InProgress bloquean el calendario.
        var hasOverlap = await reservations.HasBlockingOverlapAsync(
            command.VehicleId,
            command.Start,
            command.End,
            cancellationToken: cancellationToken);

        if (hasOverlap)
            throw new ConflictException("El vehículo ya tiene una reserva bloqueante que solapa esas fechas.", WheelbyErrorCodes.VehicleOverlap);

        var reservation = Reservation.Create(
            command.VehicleId,
            currentUser.Id,
            command.Start,
            command.End,
            vehicle.PricePerDay,
            clock.UtcNow);

        await reservations.AddAsync(reservation, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return reservation.Id;
    }
}

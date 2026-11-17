using AccessControl.Contracts;
using MediatR;
using Shared.Application.Abstractions;
using SharedKernel.Exceptions;
using Wheelby.Application.Abstractions;
using Wheelby.Domain.Errors;
using Wheelby.Domain.Reviews;

namespace Wheelby.Application.Features.CreateReview;

// Una reseña por participante por reserva Completed; no se edita ni se borra.
internal sealed class CreateReviewCommandHandler(
    IReservationRepository reservations,
    IVehicleRepository vehicles,
    IReviewRepository reviews,
    IWheelbyUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<CreateReviewCommand, Guid>
{
    public async Task<Guid> Handle(CreateReviewCommand command, CancellationToken cancellationToken)
    {
        var reservation = await reservations.GetByIdAsync(command.ReservationId, cancellationToken)
            ?? throw new NotFoundException("La reserva no existe.", WheelbyErrorCodes.ReservationNotFound);

        var vehicle = await vehicles.GetByIdAsync(reservation.VehicleId, cancellationToken)
            ?? throw new NotFoundException("La reserva no existe.", WheelbyErrorCodes.ReservationNotFound);

        // Un tercero no distingue esta reserva de una inexistente.
        if (reservation.RenterId != currentUser.Id && vehicle.OwnerId != currentUser.Id)
            throw new NotFoundException("La reserva no existe.", WheelbyErrorCodes.ReservationNotFound);

        if (await reviews.ExistsAsync(reservation.Id, currentUser.Id, cancellationToken))
            throw new ConflictException("Ya dejaste una reseña para esta reserva.", WheelbyErrorCodes.ReviewDuplicate);

        var review = Review.Create(reservation, vehicle.OwnerId, currentUser.Id, command.Rating, command.Comment, clock.UtcNow);

        await reviews.AddAsync(review, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return review.Id;
    }
}

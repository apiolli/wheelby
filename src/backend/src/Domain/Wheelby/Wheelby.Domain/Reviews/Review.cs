using SharedKernel.Domain;
using SharedKernel.Exceptions;
using Wheelby.Domain.Errors;
using Wheelby.Domain.Reservations;

namespace Wheelby.Domain.Reviews;

// Reseña de una reserva Completed. Una por actor (inquilino y anfitrión) por
// reserva, y no se edita: no hay métodos que la cambien después de crearla.
// El inquilino reseña el vehículo y al anfitrión; el anfitrión, al inquilino.
public sealed class Review : AggregateRoot<Guid>
{
    public const int RatingMin = 1;
    public const int RatingMax = 5;
    public const int CommentMaxLength = 1000;

    public Guid ReservationId { get; private set; }
    public Guid VehicleId { get; private set; }
    public Guid AuthorId { get; private set; }
    public ReservationActor AuthorRole { get; private set; }
    public Guid SubjectId { get; private set; }
    public int Rating { get; private set; }
    public string? Comment { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Review() { }

    private Review(
        Guid reservationId, Guid vehicleId, Guid authorId, ReservationActor authorRole,
        Guid subjectId, int rating, string? comment, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        ReservationId = reservationId;
        VehicleId = vehicleId;
        AuthorId = authorId;
        AuthorRole = authorRole;
        SubjectId = subjectId;
        Rating = rating;
        Comment = comment;
        CreatedAt = utcNow;
    }

    public static Review Create(
        Reservation reservation,
        Guid ownerId,
        Guid authorId,
        int rating,
        string? comment,
        DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(reservation);

        if (reservation.Status != ReservationStatus.Completed)
            throw new ConflictException(
                "Solo se puede reseñar una reserva completada.", WheelbyErrorCodes.ReviewNotAllowed);

        ReservationActor role;
        Guid subject;
        if (authorId == reservation.RenterId)
        {
            role = ReservationActor.Renter;
            subject = ownerId;
        }
        else if (authorId == ownerId)
        {
            role = ReservationActor.Host;
            subject = reservation.RenterId;
        }
        else
        {
            throw new ForbiddenException(
                "Solo los participantes de la reserva pueden reseñarla.", WheelbyErrorCodes.ReservationNotParticipant);
        }

        if (rating < RatingMin || rating > RatingMax)
            throw new BadRequestException(
                $"La calificación debe estar entre {RatingMin} y {RatingMax}.", WheelbyErrorCodes.ReviewInvalidData);

        var text = comment?.Trim();
        if (text?.Length > CommentMaxLength)
            throw new BadRequestException(
                $"El comentario no puede exceder los {CommentMaxLength} caracteres.", WheelbyErrorCodes.ReviewInvalidData);

        return new Review(
            reservation.Id, reservation.VehicleId, authorId, role, subject, rating,
            string.IsNullOrEmpty(text) ? null : text, utcNow);
    }
}

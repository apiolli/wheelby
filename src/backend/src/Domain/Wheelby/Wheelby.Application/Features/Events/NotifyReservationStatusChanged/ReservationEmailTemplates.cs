using System.Globalization;
using Notifications.Contracts;
using Wheelby.Application.DTOs;
using Wheelby.Domain.Reservations;
using Wheelby.Domain.Reservations.Events;

namespace Wheelby.Application.Features.NotifyReservationStatusChanged;

// Tipo, título y mensaje de cada aviso de reserva. Texto plano, en español.
// El motor de notificaciones agrega el saludo y la línea final del correo.
internal static class ReservationEmailTemplates
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-DO");

    public static NotificationMessage Build(
        ReservationStatusChanged change,
        ReservationSummaryDTO r,
        string? renterName)
    {
        var vehicle = $"{r.Brand} {r.Model} {r.Year} ({r.Plate})";
        var dates = $"del {r.StartDate.ToString("dd/MM/yyyy HH:mm", Es)} al {r.EndDate.ToString("dd/MM/yyyy HH:mm", Es)} (UTC)";
        var total = $"{r.TotalAmount.ToString("N2", Es)} {r.Currency}";
        var reason = string.IsNullOrWhiteSpace(change.Reason) ? "" : $"\n\nMotivo: {change.Reason}";
        var renter = string.IsNullOrWhiteSpace(renterName) ? "Un usuario" : renterName;

        var (type, title, lead) = NoticeFor(change) switch
        {
            Notice.Requested => (NotificationTypes.ReservationRequested, "Nueva solicitud de reserva",
                $"{renter} solicitó reservar tu {vehicle}, {dates}. Total: {total}. Revísala para confirmarla o rechazarla."),
            Notice.Confirmed => (NotificationTypes.ReservationConfirmed, "Tu reserva fue confirmada",
                $"El anfitrión confirmó tu reserva del {vehicle}, {dates}. Total: {total}."),
            Notice.Rejected => (NotificationTypes.ReservationRejected, "Tu solicitud de reserva fue rechazada",
                $"El anfitrión rechazó tu solicitud para el {vehicle}, {dates}."),
            Notice.CancelledByRenter => (NotificationTypes.ReservationCancelled, "Una reserva fue cancelada",
                $"El inquilino canceló la reserva de tu {vehicle}, {dates}."),
            Notice.CancelledByHost => (NotificationTypes.ReservationCancelled, "Tu reserva fue cancelada",
                $"El anfitrión canceló tu reserva del {vehicle}, {dates}."),
            Notice.Expired => (NotificationTypes.ReservationExpired, "Una solicitud de reserva expiró",
                $"La solicitud de reserva del {vehicle}, {dates}, expiró porque llegó la fecha de inicio sin confirmarse."),
            Notice.Started => (NotificationTypes.ReservationStarted, "Tu alquiler comenzó",
                $"El alquiler del {vehicle} está en curso, {dates}."),
            Notice.Completed => (NotificationTypes.ReservationCompleted, "Tu alquiler terminó",
                $"El alquiler del {vehicle} se completó. Ya puedes dejar una reseña."),
            _ => ("reservation.changed", "Tu reserva cambió de estado",
                $"La reserva del {vehicle}, {dates}, ahora está en estado {change.To}.")
        };

        return new NotificationMessage(type, $"Wheelby: {title}", $"{lead}{reason}\n\nReserva: {r.Id}", r.Id);
    }

    // Aviso por la forma de la transición. Pending -> Cancelled por el anfitrión es rechazo.
    public static Notice Kind(ReservationStatusChanged c) => (c.From, c.To, c.Actor) switch
    {
        (null, ReservationStatus.Pending, _) => Notice.Requested,
        (_, ReservationStatus.Confirmed, _) => Notice.Confirmed,
        (ReservationStatus.Pending, ReservationStatus.Cancelled, ReservationActor.Host) => Notice.Rejected,
        (_, ReservationStatus.Cancelled, ReservationActor.System) => Notice.Expired,
        (_, ReservationStatus.Cancelled, ReservationActor.Renter) => Notice.CancelledByRenter,
        (_, ReservationStatus.Cancelled, ReservationActor.Host) => Notice.CancelledByHost,
        (_, ReservationStatus.InProgress, _) => Notice.Started,
        (_, ReservationStatus.Completed, _) => Notice.Completed,
        _ => Notice.Other
    };

    // RF-NOT-03: el anfitrión que cancela con /cancel una Pending no la rechaza:
    // solo el evento marcado IsRejection (/reject) es reservation.rejected.
    public static Notice NoticeFor(ReservationStatusChanged c)
    {
        var kind = Kind(c);
        return kind == Notice.Rejected && !c.IsRejection ? Notice.CancelledByHost : kind;
    }

    public enum Notice { Requested, Confirmed, Rejected, CancelledByRenter, CancelledByHost, Expired, Started, Completed, Other }
}

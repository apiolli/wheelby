using System.Globalization;
using Wheelby.Application.DTOs;
using Wheelby.Domain.Reservations;
using Wheelby.Domain.Reservations.Events;

namespace Wheelby.Application.Features.NotifyReservationStatusChanged;

// Asunto y cuerpo de cada aviso. Texto plano, en español.
internal static class ReservationEmailTemplates
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-DO");

    public static (string Subject, string Body) Build(
        ReservationStatusChanged change,
        ReservationSummaryDTO r,
        string recipientName)
    {
        var vehicle = $"{r.Brand} {r.Model} {r.Year} ({r.Plate})";
        var dates = $"del {r.StartDate.ToString("dd/MM/yyyy HH:mm", Es)} al {r.EndDate.ToString("dd/MM/yyyy HH:mm", Es)} (UTC)";
        var total = $"{r.TotalAmount.ToString("N2", Es)} {r.Currency}";
        var reason = string.IsNullOrWhiteSpace(change.Reason) ? "" : $"\n\nMotivo: {change.Reason}";

        var (subject, lead) = Kind(change) switch
        {
            Notice.Requested => ("Nueva solicitud de reserva",
                $"Recibiste una solicitud de reserva para tu {vehicle}, {dates}. Total: {total}. Revísala para confirmarla o rechazarla."),
            Notice.Confirmed => ("Tu reserva fue confirmada",
                $"El anfitrión confirmó tu reserva del {vehicle}, {dates}. Total: {total}."),
            Notice.Rejected => ("Tu solicitud de reserva fue rechazada",
                $"El anfitrión rechazó tu solicitud para el {vehicle}, {dates}."),
            Notice.CancelledByRenter => ("Una reserva fue cancelada",
                $"El inquilino canceló la reserva de tu {vehicle}, {dates}."),
            Notice.CancelledByHost => ("Tu reserva fue cancelada",
                $"El anfitrión canceló tu reserva del {vehicle}, {dates}."),
            Notice.Expired => ("Una solicitud de reserva expiró",
                $"La solicitud de reserva del {vehicle}, {dates}, expiró porque llegó la fecha de inicio sin confirmarse."),
            Notice.Started => ("Tu alquiler comenzó",
                $"El alquiler del {vehicle} está en curso, {dates}."),
            Notice.Completed => ("Tu alquiler terminó",
                $"El alquiler del {vehicle} se completó. Ya puedes dejar una reseña."),
            _ => ("Tu reserva cambió de estado",
                $"La reserva del {vehicle}, {dates}, ahora está en estado {change.To}.")
        };

        return ($"Wheelby: {subject}", $"Hola, {recipientName}:\n\n{lead}{reason}\n\nReserva: {r.Id}\n\n— Wheelby");
    }

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

    public enum Notice { Requested, Confirmed, Rejected, CancelledByRenter, CancelledByHost, Expired, Started, Completed, Other }
}

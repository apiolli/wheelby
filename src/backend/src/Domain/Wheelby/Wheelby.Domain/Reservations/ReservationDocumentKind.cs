namespace Wheelby.Domain.Reservations;

// Momento del alquiler al que pertenece el documento (RF-NEG-10).
public enum ReservationDocumentKind
{
    Delivery = 0,   // Entrega
    Return = 1      // Devolución
}

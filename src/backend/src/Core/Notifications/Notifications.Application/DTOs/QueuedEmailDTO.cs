namespace Notifications.Application.DTOs;

// RF-NOT-11: estado de un correo de la cola para el Administrador.
// No lleva el cuerpo (CA-NOT-11.2): los correos de cuenta traen enlaces y códigos
// de un solo uso de otros usuarios.
public sealed record QueuedEmailDTO(
    Guid Id,
    string To,
    string Subject,
    string Status,
    int Attempts,
    DateTime CreatedAt,
    DateTime? LastAttemptAt,
    DateTime? SentAt,
    DateTime? FailedAt,
    string? LastError);

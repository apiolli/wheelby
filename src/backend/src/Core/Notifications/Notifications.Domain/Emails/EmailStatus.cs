namespace Notifications.Domain.Emails.Enums;

public enum EmailStatus
{
    // Pending: en la cola, se intenta en cada pasada.
    // Sent: enviado. Failed: agotó los intentos (RF-NOT-10). Los dos últimos son terminales.
    Pending,
    Sent,
    Failed
}

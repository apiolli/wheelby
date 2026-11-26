namespace Notifications.Application.Abstractions;

// La lanza el IEmailSender con un mensaje ya saneado: sin credenciales ni opciones SMTP.
// Es lo que queda en LastError del correo.
public sealed class EmailSendFailedException(string message, Exception? innerException = null)
    : Exception(message, innerException);

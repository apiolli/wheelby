using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notifications.Application.Abstractions;
using Notifications.Application.Options;
using Notifications.Domain.Emails;
using Shared.Application.Abstractions;

namespace Notifications.Application.Features.SendPendingEmails;

internal sealed class SendPendingEmailsCommandHandler(
    IQueuedEmailRepository repository,
    IEmailSender sender,
    IClock clock,
    IOptions<NotificationOptions> options,
    ILogger<SendPendingEmailsCommandHandler> logger)
    : IRequestHandler<SendPendingEmailsCommand, SendPendingEmailsResult>
{
    private const int BatchSize = 50;

    public async Task<SendPendingEmailsResult> Handle(
        SendPendingEmailsCommand request, CancellationToken cancellationToken)
    {
        var maxAttempts = options.Value.MaxEmailAttempts;
        var pending = await repository.GetPendingAsync(BatchSize, cancellationToken);

        var sent = 0;
        var failed = 0;

        foreach (var email in pending)
        {
            try
            {
                await sender.SendAsync(email.To.Value, email.Subject, email.Body, cancellationToken);
                email.MarkAsSent(clock.UtcNow);
                sent++;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                // RF-NOT-10: suma el intento y guarda el error. Un fallo no corta el lote.
                // Se registra el id y el tipo de error, nunca el cuerpo ni el enlace.
                email.RegisterFailure(Describe(ex), clock.UtcNow, maxAttempts);
                failed++;
                logger.LogWarning("Email {EmailId} could not be sent (attempt {Attempt} of {Max}): {Reason}",
                    email.Id, email.Attempts, maxAttempts, ex.GetType().Name);
            }

            // Se guarda después de cada correo: si el proceso se cae a mitad del lote,
            // los ya enviados no se reenvían.
            await repository.SaveChangesAsync(cancellationToken);
        }

        return new SendPendingEmailsResult(sent, failed);
    }

    // Tipo y mensaje de la excepción, recortados por el dominio a 500 caracteres.
    // El remitente ya sanea su mensaje (EmailSendFailedException); nunca se usan las opciones SMTP.
    internal static string Describe(Exception exception)
    {
        var text = exception is EmailSendFailedException
            ? exception.Message
            : $"{exception.GetType().Name}: {exception.Message}";

        return text.Length <= QueuedEmail.LastErrorMaxLength ? text : text[..QueuedEmail.LastErrorMaxLength];
    }
}

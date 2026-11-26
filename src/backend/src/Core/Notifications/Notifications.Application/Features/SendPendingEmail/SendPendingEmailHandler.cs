using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notifications.Application.Abstractions;
using Notifications.Application.Options;
using Notifications.Domain.Emails;
using Shared.Application.Abstractions;

namespace Notifications.Application.Features.SendPendingEmails;

// Una pasada del procesador de la cola (RF-NOT-09 a 12).
// Cada correo se toma con bloqueo de fila en su propia transacción: el Worker continuo
// y un --once simultáneo nunca envían el mismo correo, y si el proceso cae a mitad
// de la pasada, lo ya confirmado no se reenvía.
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
        // Los que ya se intentaron en esta pasada: un fallo sigue Pending y no se repite aquí.
        var processed = new List<Guid>();

        var sent = 0;
        var failed = 0;

        while (processed.Count < BatchSize)
        {
            await using var transaction = await repository.BeginTransactionAsync(cancellationToken);

            var email = await repository.ClaimNextPendingAsync(processed, cancellationToken);
            if (email is null)
                break;

            processed.Add(email.Id);

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

            await repository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        return new SendPendingEmailsResult(sent, failed);
    }

    // Tipo y mensaje de la excepción, recortados a 500 caracteres. El remitente ya sanea
    // su mensaje (EmailSendFailedException); nunca se usan las opciones SMTP.
    internal static string Describe(Exception exception)
    {
        var text = exception is EmailSendFailedException
            ? exception.Message
            : $"{exception.GetType().Name}: {exception.Message}";

        return text.Length <= QueuedEmail.LastErrorMaxLength ? text : text[..QueuedEmail.LastErrorMaxLength];
    }
}

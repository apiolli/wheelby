using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Notifications.Application.Abstractions;

namespace Notifications.Infrastructure.Email;

internal sealed class MailKitEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    private readonly SmtpOptions _smtp = options.Value;

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_smtp.From));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient { Timeout = 15_000 };

        try
        {
            await client.ConnectAsync(_smtp.Host, _smtp.Port, SecureSocketOptions.StartTls, cancellationToken);
            await client.AuthenticateAsync(_smtp.User, _smtp.Password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            // RF-NOT-10 / RD-10: el error que se guarda en la cola nunca lleva credenciales.
            throw new EmailSendFailedException($"{ex.GetType().Name}: {Sanitize(ex.Message)}", ex);
        }
    }

    private string Sanitize(string text)
    {
        foreach (var secret in new[] { _smtp.Password, _smtp.User })
        {
            if (!string.IsNullOrEmpty(secret))
                text = text.Replace(secret, "***", StringComparison.OrdinalIgnoreCase);
        }

        return text;
    }
}

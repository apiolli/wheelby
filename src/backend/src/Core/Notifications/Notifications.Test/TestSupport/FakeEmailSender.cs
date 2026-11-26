using Notifications.Application.Abstractions;

namespace Notifications.Test.TestSupport;

// Remitente de prueba: registra cada envío y falla para las direcciones indicadas
// (o para todas, si FailAll es true). Nunca abre una conexión.
public sealed class FakeEmailSender : IEmailSender
{
    public List<(string To, string Subject, string Body)> Sent { get; } = [];
    public HashSet<string> FailingAddresses { get; } = new(StringComparer.OrdinalIgnoreCase);
    public bool FailAll { get; set; }
    public int Calls { get; private set; }

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        Calls++;
        if (FailAll || FailingAddresses.Contains(to))
            throw new EmailSendFailedException("SocketException: Connection refused");

        Sent.Add((to, subject, body));
        return Task.CompletedTask;
    }
}

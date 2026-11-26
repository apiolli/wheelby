using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notifications.Application.Features.SendPendingEmails;
using Notifications.Application.Options;
using Notifications.Domain.Emails.Enums;
using Notifications.Test.TestSupport;

namespace Notifications.Test.Application;

public sealed class SendPendingEmailsHandlerTests
{
    private static readonly DateTime Creado = new(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Pasada = new(2026, 11, 20, 10, 0, 10, DateTimeKind.Utc);

    private static SendPendingEmailsCommandHandler Handler(
        FakeQueuedEmailRepository repository, FakeEmailSender sender, int maxAttempts = 5)
        => new(
            repository,
            sender,
            new FixedClock(Pasada),
            Options.Create(new NotificationOptions { MaxEmailAttempts = maxAttempts }),
            NullLogger<SendPendingEmailsCommandHandler>.Instance);

    [Fact]
    public async Task RF_NOT_10_fallo_incrementa_intentos_y_guarda_ultimo_error()
    {
        // Arrange
        var email = Emails.Pendiente("ana@wheelby.dev", Creado);
        var repository = new FakeQueuedEmailRepository(email);
        var sender = new FakeEmailSender { FailAll = true };
        var handler = Handler(repository, sender);

        // Act
        var result = await handler.Handle(new SendPendingEmailsCommand(), CancellationToken.None);

        // Assert
        Assert.Equal(new SendPendingEmailsResult(0, 1), result);
        Assert.Equal(EmailStatus.Pending, email.Status);
        Assert.Equal(1, email.Attempts);
        Assert.Equal(Pasada, email.LastAttemptAt);
        Assert.Equal("SocketException: Connection refused", email.LastError);
    }

    [Fact]
    public async Task RF_NOT_10_al_llegar_al_maximo_queda_fallido()
    {
        // Arrange
        var email = Emails.Pendiente("ana@wheelby.dev", Creado);
        var repository = new FakeQueuedEmailRepository(email);
        var sender = new FakeEmailSender { FailAll = true };
        var handler = Handler(repository, sender, maxAttempts: 5);

        // Act
        for (var pasada = 0; pasada < 5; pasada++)
            await handler.Handle(new SendPendingEmailsCommand(), CancellationToken.None);

        // Assert
        Assert.Equal(EmailStatus.Failed, email.Status);
        Assert.Equal(5, email.Attempts);
        Assert.Equal(Pasada, email.FailedAt);
        Assert.Equal("SocketException: Connection refused", email.LastError);
    }

    [Fact]
    public async Task RF_NOT_10_correo_fallido_no_se_reintenta()
    {
        // Arrange
        var email = Emails.Pendiente("ana@wheelby.dev", Creado);
        var repository = new FakeQueuedEmailRepository(email);
        var sender = new FakeEmailSender { FailAll = true };
        var handler = Handler(repository, sender, maxAttempts: 5);
        for (var pasada = 0; pasada < 5; pasada++)
            await handler.Handle(new SendPendingEmailsCommand(), CancellationToken.None);

        // Act
        var sexta = await handler.Handle(new SendPendingEmailsCommand(), CancellationToken.None);

        // Assert
        Assert.Equal(new SendPendingEmailsResult(0, 0), sexta);
        Assert.Equal(5, sender.Calls);
        Assert.Equal(5, email.Attempts);
        Assert.Equal(EmailStatus.Failed, email.Status);
        Assert.Single(repository.Emails);
    }

    [Fact]
    public async Task RF_NOT_10_un_fallo_no_impide_enviar_los_demas()
    {
        // Arrange
        var falla = Emails.Pendiente("rebota@wheelby.dev", Creado);
        var sale = Emails.Pendiente("beto@wheelby.dev", Creado.AddSeconds(1));
        var repository = new FakeQueuedEmailRepository(falla, sale);
        var sender = new FakeEmailSender();
        sender.FailingAddresses.Add("rebota@wheelby.dev");
        var handler = Handler(repository, sender);

        // Act
        var result = await handler.Handle(new SendPendingEmailsCommand(), CancellationToken.None);

        // Assert
        Assert.Equal(new SendPendingEmailsResult(1, 1), result);
        Assert.Equal(EmailStatus.Pending, falla.Status);
        Assert.Equal(EmailStatus.Sent, sale.Status);
        Assert.Equal("beto@wheelby.dev", Assert.Single(sender.Sent).To);
    }

    [Fact]
    public async Task RF_NOT_10_el_maximo_es_configurable()
    {
        // Arrange
        var email = Emails.Pendiente("ana@wheelby.dev", Creado);
        var repository = new FakeQueuedEmailRepository(email);
        var sender = new FakeEmailSender { FailAll = true };
        var handler = Handler(repository, sender, maxAttempts: 2);

        // Act
        await handler.Handle(new SendPendingEmailsCommand(), CancellationToken.None);
        await handler.Handle(new SendPendingEmailsCommand(), CancellationToken.None);

        // Assert
        Assert.Equal(EmailStatus.Failed, email.Status);
        Assert.Equal(2, email.Attempts);
    }

    [Fact]
    public void RF_NOT_10_describe_usa_tipo_y_mensaje_recortados()
    {
        // Arrange
        var exception = new InvalidOperationException(new string('a', 700));

        // Act
        var texto = SendPendingEmailsCommandHandler.Describe(exception);

        // Assert
        Assert.StartsWith("InvalidOperationException: ", texto);
        Assert.Equal(500, texto.Length);
    }
}

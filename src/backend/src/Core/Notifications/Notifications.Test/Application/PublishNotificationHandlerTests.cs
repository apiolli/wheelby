using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notifications.Application.Features.EnqueueEmail;
using Notifications.Application.Features.PublishNotification;
using Notifications.Application.Features.SendPendingEmails;
using Notifications.Application.Options;
using Notifications.Domain.Emails.Enums;
using Notifications.Domain.Inbox;
using Notifications.Infrastructure.Persistence;
using Notifications.Test.TestSupport;

namespace Notifications.Test.Application;

public sealed class PublishNotificationHandlerTests
{
    private static readonly DateTime Ahora = new(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid AnaId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid BetoId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003");
    private static readonly Guid ReservaId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");

    private static FakeUserDirectory Usuarios() => new FakeUserDirectory()
        .Con(AnaId, "Ana Pérez", "ana@wheelby.dev")
        .Con(BetoId, "Beto Gómez", "beto@wheelby.dev");

    private static PublishNotificationCommandHandler Handler(NotificationsDbContext db, FakeUserDirectory users)
        => new(
            new NotificationRepository(db),
            new QueuedEmailRepository(db),
            new NotificationPreferenceRepository(db),
            new NotificationsUnitOfWork(db),
            users,
            new FixedClock(Ahora),
            NullLogger<PublishNotificationCommandHandler>.Instance);

    private static PublishNotificationCommand Comando(params Guid[] recipients)
        => new("reservation.requested", "Nueva solicitud de reserva", "Beto solicitó tu Toyota Corolla.", ReservaId, recipients);

    [Fact]
    public async Task RF_NOT_06_notificacion_crea_entrada_en_bandeja_y_correo_encolado()
    {
        // Arrange
        var db = NotificationsDb.CreaContexto();
        var handler = Handler(db, Usuarios());

        // Act
        var creadas = await handler.Handle(Comando(AnaId), CancellationToken.None);

        // Assert
        Assert.Equal(1, creadas);
        var notificacion = Assert.Single(db.Notifications);
        var correo = Assert.Single(db.QueuedEmails);
        Assert.Equal(AnaId, notificacion.RecipientId);
        Assert.Equal("reservation.requested", notificacion.Type);
        Assert.Equal(ReservaId, notificacion.ReferenceId);
        Assert.Equal(correo.Id, notificacion.EmailId);
        Assert.Equal("ana@wheelby.dev", correo.To.Value);
        Assert.Equal("Nueva solicitud de reserva", correo.Subject);
        Assert.Contains("Beto solicitó tu Toyota Corolla.", correo.Body);
        Assert.Contains("Puedes ver tus notificaciones en Wheelby.", correo.Body);
        Assert.Equal(EmailStatus.Pending, correo.Status);
    }

    [Fact]
    public async Task RF_NOT_06_cada_destinatario_recibe_su_notificacion_y_su_correo()
    {
        // Arrange
        var db = NotificationsDb.CreaContexto();
        var handler = Handler(db, Usuarios());

        // Act
        await handler.Handle(Comando(AnaId, BetoId, AnaId), CancellationToken.None);

        // Assert
        Assert.Equal(new[] { AnaId, BetoId }.OrderBy(x => x), db.Notifications.Select(n => n.RecipientId).OrderBy(x => x));
        Assert.Equal(2, db.QueuedEmails.Count());
    }

    [Fact]
    public async Task RF_NOT_07_correo_desactivado_crea_notificacion_sin_encolar_correo()
    {
        // Arrange
        var db = NotificationsDb.CreaContexto();
        db.NotificationPreferences.Add(NotificationPreference.Create(AnaId, false, Ahora));
        await db.SaveChangesAsync();
        var handler = Handler(db, Usuarios());

        // Act
        await handler.Handle(Comando(AnaId), CancellationToken.None);

        // Assert
        var notificacion = Assert.Single(db.Notifications);
        Assert.Null(notificacion.EmailId);
        Assert.Empty(db.QueuedEmails);
    }

    [Fact]
    public async Task RF_NOT_07_correos_de_cuenta_ignoran_la_preferencia()
    {
        // Arrange
        var db = NotificationsDb.CreaContexto();
        db.NotificationPreferences.Add(NotificationPreference.Create(AnaId, false, Ahora));
        await db.SaveChangesAsync();
        var handler = new EnqueueEmailCommandHandler(new QueuedEmailRepository(db), new FixedClock(Ahora));

        // Act
        await handler.Handle(
            new EnqueueEmailCommand("ana@wheelby.dev", "Wheelby: recupera tu contraseña", "Tu código es 123456"),
            CancellationToken.None);

        // Assert
        Assert.Equal("ana@wheelby.dev", Assert.Single(db.QueuedEmails).To.Value);
        Assert.Empty(db.Notifications);
    }

    [Fact]
    public async Task RF_NOT_06_un_destinatario_inexistente_o_deshabilitado_se_omite()
    {
        // Arrange
        var db = NotificationsDb.CreaContexto();
        var handler = Handler(db, Usuarios());

        // Act
        var creadas = await handler.Handle(Comando(Guid.NewGuid(), BetoId), CancellationToken.None);

        // Assert
        Assert.Equal(1, creadas);
        Assert.Equal(BetoId, Assert.Single(db.Notifications).RecipientId);
    }

    [Fact]
    public async Task RF_NOT_08_operacion_termina_aunque_el_envio_falle()
    {
        // Arrange
        var db = NotificationsDb.CreaContexto();
        var handler = Handler(db, Usuarios());
        var sender = new FakeEmailSender { FailAll = true };

        // Act
        var exception = await Record.ExceptionAsync(() => handler.Handle(Comando(AnaId), CancellationToken.None));

        // Assert: publicar no llama al servidor de correo; el correo espera Pending al procesador.
        Assert.Null(exception);
        Assert.Equal(0, sender.Calls);
        var correo = Assert.Single(db.QueuedEmails);
        Assert.Equal(EmailStatus.Pending, correo.Status);
        Assert.Equal(0, correo.Attempts);
    }

    [Fact]
    public async Task RF_NOT_08_el_fallo_del_procesador_no_afecta_la_notificacion()
    {
        // Arrange
        var db = NotificationsDb.CreaContexto();
        await Handler(db, Usuarios()).Handle(Comando(AnaId), CancellationToken.None);
        var correo = Assert.Single(db.QueuedEmails);
        var repository = new FakeQueuedEmailRepository(correo);
        var procesador = new SendPendingEmailsCommandHandler(
            repository, new FakeEmailSender { FailAll = true }, new FixedClock(Ahora),
            Options.Create(new NotificationOptions { MaxEmailAttempts = 5 }),
            NullLogger<SendPendingEmailsCommandHandler>.Instance);

        // Act
        await procesador.Handle(new SendPendingEmailsCommand(), CancellationToken.None);

        // Assert
        Assert.Equal(1, correo.Attempts);
        Assert.Single(db.Notifications);
    }

    [Fact]
    public async Task RF_NOT_06_sin_destinatarios_no_guarda_nada()
    {
        // Arrange
        var db = NotificationsDb.CreaContexto();
        var handler = Handler(db, Usuarios());

        // Act
        var creadas = await handler.Handle(Comando(), CancellationToken.None);

        // Assert
        Assert.Equal(0, creadas);
        Assert.Empty(db.Notifications);
        Assert.Empty(db.QueuedEmails);
    }
}

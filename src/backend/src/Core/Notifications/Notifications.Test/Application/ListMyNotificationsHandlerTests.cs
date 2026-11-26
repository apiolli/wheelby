using Notifications.Application.Features.ListMyNotifications;
using Notifications.Domain.Inbox;
using Notifications.Infrastructure.Persistence;
using Notifications.Test.TestSupport;

namespace Notifications.Test.Application;

public sealed class ListMyNotificationsHandlerTests
{
    private static readonly DateTime T0 = new(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid AnaId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid AdminId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");

    private static Notification Nueva(Guid recipientId, DateTime createdAt, string title = "Título")
        => Notification.Create(recipientId, "permission.request-created", title, "Mensaje", null, createdAt);

    [Fact]
    public async Task RF_NOT_04_un_usuario_no_ve_notificaciones_de_otro()
    {
        // Arrange
        var db = NotificationsDb.CreaContexto();
        db.Notifications.AddRange(Nueva(AnaId, T0), Nueva(AdminId, T0.AddMinutes(1)), Nueva(AdminId, T0.AddMinutes(2)));
        await db.SaveChangesAsync();
        var store = new NotificationReadStore(db);
        var deAna = new ListMyNotificationsQueryHandler(store, new FixedCurrentUser(AnaId));
        var delAdmin = new ListMyNotificationsQueryHandler(store, new FixedCurrentUser(AdminId));

        // Act
        var paginaAna = await deAna.Handle(new ListMyNotificationsQuery(null, 1, 100), CancellationToken.None);
        var otraPaginaAna = await deAna.Handle(new ListMyNotificationsQuery(false, 2, 1), CancellationToken.None);
        var paginaAdmin = await delAdmin.Handle(new ListMyNotificationsQuery(null, 1, 100), CancellationToken.None);

        // Assert
        Assert.All(paginaAna.Items, n => Assert.Equal(AnaId, n.RecipientId));
        Assert.Single(paginaAna.Items);
        Assert.Empty(otraPaginaAna.Items);
        Assert.Equal(2, paginaAdmin.TotalItems);
        Assert.Empty(paginaAna.Items.Select(n => n.Id).Intersect(paginaAdmin.Items.Select(n => n.Id)));
    }

    [Fact]
    public async Task RF_NOT_04_filtra_leidas_y_no_leidas_y_cuenta_no_leidas()
    {
        // Arrange
        var db = NotificationsDb.CreaContexto();
        var vieja = Nueva(AnaId, T0, "vieja");
        var media = Nueva(AnaId, T0.AddMinutes(1), "media");
        var nueva = Nueva(AnaId, T0.AddMinutes(2), "nueva");
        media.MarkAsRead(T0.AddMinutes(5));
        db.Notifications.AddRange(vieja, media, nueva);
        await db.SaveChangesAsync();
        var handler = new ListMyNotificationsQueryHandler(new NotificationReadStore(db), new FixedCurrentUser(AnaId));

        // Act
        var todas = await handler.Handle(new ListMyNotificationsQuery(null), CancellationToken.None);
        var noLeidas = await handler.Handle(new ListMyNotificationsQuery(false), CancellationToken.None);
        var leidas = await handler.Handle(new ListMyNotificationsQuery(true), CancellationToken.None);

        // Assert
        Assert.Equal(new[] { "nueva", "media", "vieja" }, todas.Items.Select(n => n.Title));
        Assert.Equal(2, noLeidas.Items.Count);
        Assert.Equal("media", Assert.Single(leidas.Items).Title);
        Assert.Equal(2, todas.UnreadCount);
        Assert.Equal(2, leidas.UnreadCount);
        var leida = leidas.Items[0];
        Assert.True(leida.IsRead);
        Assert.Equal(T0.AddMinutes(5), leida.ReadAt);
        Assert.All(noLeidas.Items, n => { Assert.False(n.IsRead); Assert.Null(n.ReadAt); });
    }

    [Fact]
    public async Task RF_NOT_06_email_sent_indica_si_hay_correo_enlazado()
    {
        // Arrange
        var db = NotificationsDb.CreaContexto();
        var conCorreo = Nueva(AnaId, T0, "con");
        conCorreo.AttachEmail(Guid.NewGuid());
        db.Notifications.AddRange(conCorreo, Nueva(AnaId, T0.AddMinutes(1), "sin"));
        await db.SaveChangesAsync();
        var handler = new ListMyNotificationsQueryHandler(new NotificationReadStore(db), new FixedCurrentUser(AnaId));

        // Act
        var page = await handler.Handle(new ListMyNotificationsQuery(null), CancellationToken.None);

        // Assert
        Assert.True(page.Items.Single(n => n.Title == "con").EmailSent);
        Assert.False(page.Items.Single(n => n.Title == "sin").EmailSent);
    }
}

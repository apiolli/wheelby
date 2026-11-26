using Notifications.Application.Features.MarkNotificationAsRead;
using Notifications.Domain.Inbox;
using Notifications.Infrastructure.Persistence;
using Notifications.Test.TestSupport;
using SharedKernel.Exceptions;

namespace Notifications.Test.Application;

public sealed class MarkNotificationAsReadHandlerTests
{
    private static readonly DateTime Creada = new(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Lectura = new(2026, 11, 20, 11, 0, 0, DateTimeKind.Utc);
    private static readonly Guid AnaId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid AdminId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");

    private static MarkNotificationAsReadCommandHandler Handler(
        NotificationsDbContext db, Guid userId, FixedClock clock)
        => new(new NotificationRepository(db), new NotificationsUnitOfWork(db), new FixedCurrentUser(userId), clock);

    private static async Task<(NotificationsDbContext Db, Notification DeAna, Notification DelAdmin)> CreaAsync()
    {
        var db = NotificationsDb.CreaContexto();
        var deAna = Notification.Create(AnaId, "tipo", "Título", "Mensaje", null, Creada);
        var delAdmin = Notification.Create(AdminId, "tipo", "Título", "Mensaje", null, Creada);
        db.Notifications.AddRange(deAna, delAdmin);
        await db.SaveChangesAsync();
        return (db, deAna, delAdmin);
    }

    [Fact]
    public async Task RF_NOT_05_marcar_propia_como_leida()
    {
        // Arrange
        var (db, deAna, _) = await CreaAsync();
        var handler = Handler(db, AnaId, new FixedClock(Lectura));

        // Act
        await handler.Handle(new MarkNotificationAsReadCommand(deAna.Id), CancellationToken.None);

        // Assert
        var guardada = await db.Notifications.FindAsync(deAna.Id);
        Assert.True(guardada!.IsRead);
        Assert.Equal(Lectura, guardada.ReadAt);
    }

    [Fact]
    public async Task RF_NOT_05_marcar_dos_veces_conserva_la_primera_fecha()
    {
        // Arrange
        var (db, deAna, _) = await CreaAsync();
        var clock = new FixedClock(Lectura);
        var handler = Handler(db, AnaId, clock);
        await handler.Handle(new MarkNotificationAsReadCommand(deAna.Id), CancellationToken.None);
        clock.UtcNow = Lectura.AddHours(3);

        // Act
        var exception = await Record.ExceptionAsync(
            () => handler.Handle(new MarkNotificationAsReadCommand(deAna.Id), CancellationToken.None));

        // Assert
        Assert.Null(exception);
        Assert.Equal(Lectura, (await db.Notifications.FindAsync(deAna.Id))!.ReadAt);
    }

    [Fact]
    public async Task RF_NOT_05_marcar_la_de_otro_se_rechaza_y_no_cambia()
    {
        // Arrange
        var (db, _, delAdmin) = await CreaAsync();
        var handler = Handler(db, AnaId, new FixedClock(Lectura));

        // Act
        var exception = await Record.ExceptionAsync(
            () => handler.Handle(new MarkNotificationAsReadCommand(delAdmin.Id), CancellationToken.None));

        // Assert
        var notFound = Assert.IsType<NotFoundException>(exception);
        Assert.Equal("notification.not-found", notFound.Code);
        Assert.Null((await db.Notifications.FindAsync(delAdmin.Id))!.ReadAt);
    }

    [Fact]
    public async Task RF_NOT_05_un_id_que_no_existe_da_la_misma_respuesta()
    {
        // Arrange
        var (db, _, _) = await CreaAsync();
        var handler = Handler(db, AnaId, new FixedClock(Lectura));

        // Act
        var exception = await Record.ExceptionAsync(
            () => handler.Handle(new MarkNotificationAsReadCommand(Guid.NewGuid()), CancellationToken.None));

        // Assert
        var notFound = Assert.IsType<NotFoundException>(exception);
        Assert.Equal("notification.not-found", notFound.Code);
    }
}

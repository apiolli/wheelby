using Notifications.Domain.Inbox;
using Notifications.Domain.Inbox.Exceptions;

namespace Notifications.Test.Domain;

public sealed class NotificationTests
{
    private static readonly DateTime Ahora = new(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid AnaId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    [Fact]
    public void RF_NOT_04_una_notificacion_nueva_nace_sin_leer_y_sin_correo()
    {
        // Arrange
        var referencia = Guid.NewGuid();

        // Act
        var n = Notification.Create(AnaId, "reservation.requested", "Nueva solicitud", "Texto", referencia, Ahora);

        // Assert
        Assert.Equal(AnaId, n.RecipientId);
        Assert.Equal("reservation.requested", n.Type);
        Assert.Equal(referencia, n.ReferenceId);
        Assert.Equal(Ahora, n.CreatedAt);
        Assert.False(n.IsRead);
        Assert.Null(n.ReadAt);
        Assert.Null(n.EmailId);
    }

    [Theory]
    [InlineData("", "Título", "Mensaje")]
    [InlineData("tipo", " ", "Mensaje")]
    [InlineData("tipo", "Título", "")]
    public void RF_NOT_04_datos_obligatorios_vacios_se_rechazan(string type, string title, string message)
    {
        // Arrange (en los parámetros)

        // Act
        var exception = Record.Exception(() => Notification.Create(AnaId, type, title, message, null, Ahora));

        // Assert
        Assert.IsType<InvalidNotificationDataException>(exception);
    }

    [Fact]
    public void RF_NOT_04_un_titulo_de_mas_de_200_caracteres_se_rechaza()
    {
        // Arrange
        var titulo = new string('t', 201);

        // Act
        var exception = Record.Exception(() => Notification.Create(AnaId, "tipo", titulo, "Mensaje", null, Ahora));

        // Assert
        Assert.IsType<InvalidNotificationDataException>(exception);
    }

    [Fact]
    public void RF_NOT_05_marcar_como_leida_fija_la_fecha()
    {
        // Arrange
        var n = Notification.Create(AnaId, "tipo", "Título", "Mensaje", null, Ahora);

        // Act
        n.MarkAsRead(Ahora.AddHours(1));

        // Assert
        Assert.True(n.IsRead);
        Assert.Equal(Ahora.AddHours(1), n.ReadAt);
    }

    [Fact]
    public void RF_NOT_05_marcar_dos_veces_no_cambia_la_fecha()
    {
        // Arrange
        var n = Notification.Create(AnaId, "tipo", "Título", "Mensaje", null, Ahora);
        n.MarkAsRead(Ahora.AddHours(1));

        // Act
        n.MarkAsRead(Ahora.AddHours(2));

        // Assert
        Assert.Equal(Ahora.AddHours(1), n.ReadAt);
    }

    [Fact]
    public void RF_NOT_06_enlazar_el_correo_guarda_su_id()
    {
        // Arrange
        var n = Notification.Create(AnaId, "tipo", "Título", "Mensaje", null, Ahora);
        var emailId = Guid.NewGuid();

        // Act
        n.AttachEmail(emailId);

        // Assert
        Assert.Equal(emailId, n.EmailId);
    }
}

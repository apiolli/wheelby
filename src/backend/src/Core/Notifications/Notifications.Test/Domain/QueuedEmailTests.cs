using Notifications.Domain.Emails.Enums;
using Notifications.Test.TestSupport;
using SharedKernel.Exceptions;

namespace Notifications.Test.Domain;

public sealed class QueuedEmailTests
{
    private static readonly DateTime Creado = new(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Intento = new(2026, 11, 20, 10, 0, 10, DateTimeKind.Utc);

    [Fact]
    public void RF_NOT_08_un_correo_nuevo_queda_pending_sin_intentos()
    {
        // Arrange y Act
        var email = Emails.Pendiente("ana@wheelby.dev", Creado);

        // Assert
        Assert.Equal(EmailStatus.Pending, email.Status);
        Assert.Equal(0, email.Attempts);
        Assert.Null(email.LastAttemptAt);
        Assert.Null(email.LastError);
        Assert.Null(email.FailedAt);
    }

    [Fact]
    public void RF_NOT_10_marcar_enviado_cuenta_el_intento()
    {
        // Arrange
        var email = Emails.Pendiente("ana@wheelby.dev", Creado);

        // Act
        email.MarkAsSent(Intento);

        // Assert
        Assert.Equal(EmailStatus.Sent, email.Status);
        Assert.Equal(1, email.Attempts);
        Assert.Equal(Intento, email.SentAt);
        Assert.Equal(Intento, email.LastAttemptAt);
    }

    [Fact]
    public void RF_NOT_10_un_fallo_bajo_el_maximo_sigue_pending()
    {
        // Arrange
        var email = Emails.Pendiente("ana@wheelby.dev", Creado);

        // Act
        email.RegisterFailure("SocketException: Connection refused", Intento, 5);

        // Assert
        Assert.Equal(EmailStatus.Pending, email.Status);
        Assert.Equal(1, email.Attempts);
        Assert.Equal(Intento, email.LastAttemptAt);
        Assert.Equal("SocketException: Connection refused", email.LastError);
        Assert.Null(email.FailedAt);
    }

    [Fact]
    public void RF_NOT_10_el_quinto_fallo_deja_el_correo_fallido()
    {
        // Arrange
        var email = Emails.Pendiente("ana@wheelby.dev", Creado);
        for (var i = 0; i < 4; i++)
            email.RegisterFailure("error", Intento, 5);

        // Act
        email.RegisterFailure("ultimo error", Intento, 5);

        // Assert
        Assert.Equal(EmailStatus.Failed, email.Status);
        Assert.Equal(5, email.Attempts);
        Assert.Equal(Intento, email.FailedAt);
        Assert.Equal("ultimo error", email.LastError);
    }

    [Fact]
    public void RF_NOT_10_el_error_se_recorta_a_500_caracteres()
    {
        // Arrange
        var email = Emails.Pendiente("ana@wheelby.dev", Creado);
        var largo = new string('x', 800);

        // Act
        email.RegisterFailure(largo, Intento, 5);

        // Assert
        Assert.Equal(500, email.LastError!.Length);
    }

    [Fact]
    public void RF_NOT_10_un_error_vacio_guarda_un_mensaje_no_vacio()
    {
        // Arrange
        var email = Emails.Pendiente("ana@wheelby.dev", Creado);

        // Act
        email.RegisterFailure("   ", Intento, 5);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(email.LastError));
    }

    [Fact]
    public void RF_NOT_10_un_correo_fallido_no_admite_mas_cambios()
    {
        // Arrange
        var email = Emails.Pendiente("ana@wheelby.dev", Creado);
        email.RegisterFailure("error", Intento, 1);

        // Act
        var alEnviar = Record.Exception(() => email.MarkAsSent(Intento));
        var alFallar = Record.Exception(() => email.RegisterFailure("otro", Intento, 1));

        // Assert
        Assert.IsType<ConflictException>(alEnviar);
        Assert.IsType<ConflictException>(alFallar);
        Assert.Equal(1, email.Attempts);
    }

    [Fact]
    public void RF_NOT_12_un_correo_enviado_no_admite_otro_envio()
    {
        // Arrange
        var email = Emails.Pendiente("ana@wheelby.dev", Creado);
        email.MarkAsSent(Intento);

        // Act
        var exception = Record.Exception(() => email.MarkAsSent(Intento.AddMinutes(1)));

        // Assert
        Assert.IsType<ConflictException>(exception);
        Assert.Equal(Intento, email.SentAt);
        Assert.Equal(1, email.Attempts);
    }
}

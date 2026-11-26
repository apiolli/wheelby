using AccessControl.Contracts;
using Notifications.Application.Features.PublishNotification;
using Notifications.Contracts;
using Notifications.Infrastructure.Inbox;
using Notifications.Test.TestSupport;

namespace Notifications.Test.Infrastructure;

public sealed class NotificationPublisherTests
{
    private static readonly Guid AdminA = Guid.Parse("cccccccc-0000-0000-0000-00000000000a");
    private static readonly Guid AdminB = Guid.Parse("cccccccc-0000-0000-0000-00000000000b");
    private static readonly NotificationMessage Mensaje =
        new("permission.request-created", "Nueva solicitud de permiso", "Ana solicitó core.demo-action.", null);

    private static FakeAdministratorDirectory Admins() => new(
        new UserContact(AdminA, "Admin A", "a@wheelby.dev"),
        new UserContact(AdminB, "Admin B", "b@wheelby.dev"));

    [Fact]
    public async Task RF_NOT_01_publicar_a_administradores_manda_a_todos_los_activos()
    {
        // Arrange
        var sender = new RecordingSender();
        var publisher = new NotificationPublisher(sender, Admins());

        // Act
        await publisher.PublishToAdministratorsAsync(Mensaje, [Guid.NewGuid()]);

        // Assert
        var comando = Assert.IsType<PublishNotificationCommand>(Assert.Single(sender.Requests));
        Assert.Equal(new[] { AdminA, AdminB }, comando.RecipientIds);
        Assert.Equal("permission.request-created", comando.Type);
    }

    [Fact]
    public async Task RF_NOT_01_publicar_a_administradores_excluye_a_los_indicados()
    {
        // Arrange
        var sender = new RecordingSender();
        var publisher = new NotificationPublisher(sender, Admins());

        // Act
        await publisher.PublishToAdministratorsAsync(Mensaje, [AdminB]);

        // Assert
        var comando = Assert.IsType<PublishNotificationCommand>(Assert.Single(sender.Requests));
        Assert.Equal(new[] { AdminA }, comando.RecipientIds);
    }

    [Fact]
    public async Task RF_NOT_06_sin_destinatarios_no_se_manda_nada()
    {
        // Arrange
        var sender = new RecordingSender();
        var publisher = new NotificationPublisher(sender, new FakeAdministratorDirectory());

        // Act
        await publisher.PublishToAdministratorsAsync(Mensaje, []);
        await publisher.PublishAsync(Mensaje, []);

        // Assert
        Assert.Empty(sender.Requests);
    }
}

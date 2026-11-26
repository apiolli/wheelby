using Microsoft.Extensions.Logging.Abstractions;
using AccessControl.Contracts;
using Notifications.Contracts;
using Permissions.Application.Abstractions;
using Permissions.Application.Features.CreatePermissionRequest;
using Permissions.Contracts;
using Permissions.Domain.PermissionRequests;
using Permissions.Infrastructure.Notifiers;
using Permissions.Test.Support;
using SharedKernel.Exceptions;

namespace Permissions.Test.Application;

// RF-NOT-01: crear una solicitud de permiso notifica a los Administradores activos.
public sealed class PermissionRequestCreatedNotificationTests
{
    private static readonly DateTime Ahora = new(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);
    private const string Tipo = "permission.request-created";

    private static CreatePermissionRequestCommandHandler Handler(
        Guid solicitanteId, IPermissionRequestNotifier notifier,
        FakePermissionRequestRepository? requests = null)
        => new(
            requests ?? new FakePermissionRequestRepository(),
            new FakePermissionGrantRepository(),
            new FixedPermissionCatalog(PermissionCodes.DemoAction),
            new RecordingUnitOfWork(),
            new FixedCurrentUser(solicitanteId),
            new FixedClock(Ahora),
            notifier);

    [Fact]
    public async Task RF_NOT_01_crear_solicitud_notifica_a_cada_administrador_activo()
    {
        // Arrange
        var engine = new NotificationEngine(Ahora);
        var adminA = engine.Administrador("Admin A", "admin-a@wheelby.dev");
        var adminB = engine.Administrador("Admin B", "admin-b@wheelby.dev");
        var ana = engine.Estandar("Ana Pérez", "ana@wheelby.dev");

        // Act
        var id = await Handler(ana, engine.Notifier)
            .Handle(new CreatePermissionRequestCommand("core.demo-action"), CancellationToken.None);

        // Assert
        foreach (var admin in new[] { adminA, adminB })
        {
            var n = Assert.Single(engine.De(admin));
            Assert.Equal(Tipo, n.Type);
            Assert.False(n.IsRead);
            Assert.Equal(id, n.ReferenceId);
            Assert.Contains("core.demo-action", n.Message);
            Assert.Contains("Ana Pérez", n.Message);
            Assert.NotNull(n.EmailId);
        }
    }

    [Fact]
    public async Task RF_NOT_01_crear_solicitud_no_notifica_a_estandar_ni_a_administrador_deshabilitado()
    {
        // Arrange
        var engine = new NotificationEngine(Ahora);
        engine.Administrador("Admin A", "admin-a@wheelby.dev");
        engine.Administrador("Admin B", "admin-b@wheelby.dev");
        var adminC = engine.AdministradorDeshabilitado("Admin C", "admin-c@wheelby.dev");
        var sinActivar = engine.AdministradorSinActivar("Admin D", "admin-d@wheelby.dev");
        var ana = engine.Estandar("Ana Pérez", "ana@wheelby.dev");
        var beto = engine.Estandar("Beto Gómez", "beto@wheelby.dev");

        // Act
        await Handler(ana, engine.Notifier)
            .Handle(new CreatePermissionRequestCommand("core.demo-action"), CancellationToken.None);

        // Assert
        Assert.Empty(engine.De(ana));
        Assert.Empty(engine.De(beto));
        Assert.Empty(engine.De(adminC));
        Assert.Empty(engine.De(sinActivar));
        Assert.Equal(2, engine.DelTipo(Tipo).Count);
    }

    [Fact]
    public async Task RF_NOT_01_administrador_solicitante_no_recibe_su_propia_notificacion()
    {
        // Arrange
        var engine = new NotificationEngine(Ahora);
        var adminA = engine.Administrador("Admin A", "admin-a@wheelby.dev");
        var adminB = engine.Administrador("Admin B", "admin-b@wheelby.dev");

        // Act
        await Handler(adminB, engine.Notifier)
            .Handle(new CreatePermissionRequestCommand("core.demo-action"), CancellationToken.None);

        // Assert
        Assert.Single(engine.De(adminA));
        Assert.Empty(engine.De(adminB));
    }

    [Fact]
    public async Task RF_NOT_01_una_creacion_rechazada_no_notifica()
    {
        // Arrange
        var engine = new NotificationEngine(Ahora);
        engine.Administrador("Admin A", "admin-a@wheelby.dev");
        var ana = engine.Estandar("Ana Pérez", "ana@wheelby.dev");
        var pendiente = PermissionRequest.Create(ana, PermissionCodes.DemoAction, Ahora);
        var handler = Handler(ana, engine.Notifier, new FakePermissionRequestRepository(pendiente));

        // Act
        var exception = await Record.ExceptionAsync(
            () => handler.Handle(new CreatePermissionRequestCommand("core.demo-action"), CancellationToken.None));

        // Assert
        Assert.IsType<ConflictException>(exception);
        Assert.Empty(engine.Inbox.Notifications);
        Assert.Empty(engine.Inbox.QueuedEmails);
    }

    [Fact]
    public async Task RF_NOT_01_si_el_motor_falla_la_solicitud_se_crea_igual()
    {
        // Arrange
        var engine = new NotificationEngine(Ahora);
        var ana = engine.Estandar("Ana Pérez", "ana@wheelby.dev");
        var requests = new FakePermissionRequestRepository();
        var notifier = new PermissionRequestNotifier(
            new FailingNotificationPublisher(), new NullUserDirectory(),
            NullLogger<PermissionRequestNotifier>.Instance);
        var handler = Handler(ana, notifier, requests);

        // Act
        var id = await handler.Handle(new CreatePermissionRequestCommand("core.demo-action"), CancellationToken.None);

        // Assert
        Assert.Equal(id, Assert.Single(requests.Requests).Id);
    }
}

// Publicador que siempre falla, para comprobar que el aviso es un efecto secundario.
internal sealed class FailingNotificationPublisher : INotificationPublisher
{
    public Task PublishAsync(NotificationMessage message, IReadOnlyCollection<Guid> recipientIds, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("motor caído");

    public Task PublishToAdministratorsAsync(NotificationMessage message, IReadOnlyCollection<Guid> excludeUserIds, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("motor caído");
}

// Directorio vacío: nadie existe.
internal sealed class NullUserDirectory : IUserDirectory
{
    public Task<UserContact?> GetContactAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult<UserContact?>(null);
}

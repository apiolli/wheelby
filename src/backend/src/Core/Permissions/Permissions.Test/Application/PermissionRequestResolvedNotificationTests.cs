using Permissions.Application.Features.ApprovePermissionRequest;
using Permissions.Application.Features.RejectPermissionRequest;
using Permissions.Contracts;
using Permissions.Domain.PermissionRequests;
using Permissions.Test.Support;
using SharedKernel.Exceptions;

namespace Permissions.Test.Application;

// RF-NOT-02: resolver una solicitud notifica al solicitante.
public sealed class PermissionRequestResolvedNotificationTests
{
    private static readonly DateTime Creada = new(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Resuelta = new(2026, 11, 20, 13, 0, 0, DateTimeKind.Utc);

    private sealed record Escenario(
        NotificationEngine Engine, Guid AdminA, Guid AdminB, Guid Ana, PermissionRequest Solicitud,
        FakePermissionRequestRepository Requests);

    private static Escenario Crear()
    {
        var engine = new NotificationEngine(Resuelta);
        var adminA = engine.Administrador("Admin A", "admin-a@wheelby.dev");
        var adminB = engine.Administrador("Admin B", "admin-b@wheelby.dev");
        var ana = engine.Estandar("Ana Pérez", "ana@wheelby.dev");
        var solicitud = PermissionRequest.Create(ana, PermissionCodes.DemoAction, Creada);
        return new Escenario(engine, adminA, adminB, ana, solicitud, new FakePermissionRequestRepository(solicitud));
    }

    private static ApprovePermissionRequestCommandHandler Aprobar(Escenario e, Guid adminId)
        => new(e.Requests, new FakePermissionGrantRepository(), new RecordingUnitOfWork(),
            new FixedCurrentUser(adminId), new FixedClock(Resuelta), e.Engine.Notifier);

    private static RejectPermissionRequestCommandHandler Rechazar(Escenario e, Guid adminId)
        => new(e.Requests, new RecordingUnitOfWork(), new FixedCurrentUser(adminId), new FixedClock(Resuelta),
            e.Engine.Notifier);

    [Fact]
    public async Task RF_NOT_02_aprobar_notifica_solo_al_solicitante()
    {
        // Arrange
        var e = Crear();

        // Act
        await Aprobar(e, e.AdminA).Handle(new ApprovePermissionRequestCommand(e.Solicitud.Id), CancellationToken.None);

        // Assert
        var n = Assert.Single(e.Engine.De(e.Ana));
        Assert.Equal("permission.request-approved", n.Type);
        Assert.False(n.IsRead);
        Assert.Contains("aprobada", n.Message);
        Assert.Contains("core.demo-action", n.Message);
        Assert.Equal(e.Solicitud.Id, n.ReferenceId);
        Assert.Single(e.Engine.DelTipo("permission.request-approved"));
        Assert.Empty(e.Engine.De(e.AdminA));
        Assert.Empty(e.Engine.De(e.AdminB));
    }

    [Fact]
    public async Task RF_NOT_02_rechazar_notifica_al_solicitante_con_el_motivo()
    {
        // Arrange
        var e = Crear();

        // Act
        await Rechazar(e, e.AdminA).Handle(
            new RejectPermissionRequestCommand(e.Solicitud.Id, "No corresponde al perfil del usuario"),
            CancellationToken.None);

        // Assert
        var n = Assert.Single(e.Engine.De(e.Ana));
        Assert.Equal("permission.request-rejected", n.Type);
        Assert.Contains("rechazada", n.Message);
        Assert.Contains("No corresponde al perfil del usuario", n.Message);
        var correo = Assert.Single(e.Engine.Inbox.QueuedEmails);
        Assert.Equal(n.EmailId, correo.Id);
        Assert.Equal("ana@wheelby.dev", correo.To.Value);
        Assert.Contains("No corresponde al perfil del usuario", correo.Body);
    }

    [Fact]
    public async Task RF_NOT_02_transicion_rechazada_no_crea_notificacion()
    {
        // Arrange
        var e = Crear();
        e.Solicitud.Approve(e.AdminB, Creada);
        e.Solicitud.MarkApplied(Creada);

        // Act
        var alAprobar = await Record.ExceptionAsync(() =>
            Aprobar(e, e.AdminA).Handle(new ApprovePermissionRequestCommand(e.Solicitud.Id), CancellationToken.None));
        var alRechazar = await Record.ExceptionAsync(() =>
            Rechazar(e, e.AdminA).Handle(new RejectPermissionRequestCommand(e.Solicitud.Id, "Tarde"), CancellationToken.None));

        // Assert
        Assert.Equal(409, Assert.IsAssignableFrom<AppException>(alAprobar).StatusCode);
        Assert.Equal(409, Assert.IsAssignableFrom<AppException>(alRechazar).StatusCode);
        Assert.Empty(e.Engine.Inbox.Notifications);
        Assert.Empty(e.Engine.Inbox.QueuedEmails);
    }

    [Fact]
    public async Task RF_NOT_02_rechazar_sin_motivo_no_notifica()
    {
        // Arrange
        var e = Crear();

        // Act
        var exception = await Record.ExceptionAsync(() =>
            Rechazar(e, e.AdminA).Handle(new RejectPermissionRequestCommand(e.Solicitud.Id, " "), CancellationToken.None));

        // Assert
        Assert.Equal(400, Assert.IsAssignableFrom<AppException>(exception).StatusCode);
        Assert.Empty(e.Engine.Inbox.Notifications);
    }

    [Fact]
    public async Task RF_NOT_02_con_el_correo_desactivado_queda_solo_la_notificacion()
    {
        // Arrange
        var e = Crear();
        e.Engine.Inbox.NotificationPreferences.Add(
            Notifications.Domain.Inbox.NotificationPreference.Create(e.Ana, false, Creada));
        await e.Engine.Inbox.SaveChangesAsync();

        // Act
        await Aprobar(e, e.AdminA).Handle(new ApprovePermissionRequestCommand(e.Solicitud.Id), CancellationToken.None);

        // Assert
        Assert.Null(Assert.Single(e.Engine.De(e.Ana)).EmailId);
        Assert.Empty(e.Engine.Inbox.QueuedEmails);
    }
}

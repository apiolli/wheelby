using System.Text.Json;
using Notifications.Application.DTOs;
using Notifications.Application.Features.GetQueuedEmail;
using Notifications.Application.Features.ListQueuedEmails;
using Notifications.Domain.Emails.Enums;
using Notifications.Infrastructure.Persistence;
using Notifications.Test.TestSupport;
using SharedKernel.Exceptions;

namespace Notifications.Test.Infrastructure;

public sealed class QueuedEmailReadStoreTests
{
    private static readonly DateTime T0 = new(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);

    private static async Task<(QueuedEmailReadStore Store, Guid PendienteId, Guid EnviadoId, Guid FallidoId)> CreaColaAsync()
    {
        var db = NotificationsDb.CreaContexto();
        var pendiente = Emails.Pendiente("ana@wheelby.dev", T0);
        var enviado = Emails.Pendiente("beto@wheelby.dev", T0.AddMinutes(1));
        enviado.MarkAsSent(T0.AddMinutes(2));
        var fallido = Emails.Pendiente("carla@wheelby.dev", T0.AddMinutes(3));
        fallido.RegisterFailure("SocketException: Connection refused", T0.AddMinutes(4), 1);
        db.QueuedEmails.AddRange(pendiente, enviado, fallido);
        await db.SaveChangesAsync();
        return (new QueuedEmailReadStore(db), pendiente.Id, enviado.Id, fallido.Id);
    }

    [Fact]
    public async Task RF_NOT_11_administrador_consulta_la_cola_con_estado_intentos_fechas_y_error()
    {
        // Arrange
        var (store, pendienteId, enviadoId, fallidoId) = await CreaColaAsync();
        var handler = new ListQueuedEmailsQueryHandler(store);

        // Act
        var todos = await handler.Handle(new ListQueuedEmailsQuery(null, 1, 20), CancellationToken.None);
        var fallidos = await handler.Handle(new ListQueuedEmailsQuery("Failed", 1, 20), CancellationToken.None);

        // Assert
        Assert.Equal(3, todos.TotalItems);
        Assert.Equal(new[] { fallidoId, enviadoId, pendienteId }, todos.Items.Select(e => e.Id));
        var fallido = Assert.Single(fallidos.Items);
        Assert.Equal("carla@wheelby.dev", fallido.To);
        Assert.Equal("Asunto de prueba", fallido.Subject);
        Assert.Equal("Failed", fallido.Status);
        Assert.Equal(1, fallido.Attempts);
        Assert.Equal(T0.AddMinutes(3), fallido.CreatedAt);
        Assert.Equal(T0.AddMinutes(4), fallido.LastAttemptAt);
        Assert.Equal(T0.AddMinutes(4), fallido.FailedAt);
        Assert.Null(fallido.SentAt);
        Assert.Equal("SocketException: Connection refused", fallido.LastError);
    }

    [Fact]
    public async Task RF_NOT_11_filtra_por_estado_y_pagina()
    {
        // Arrange
        var (store, _, enviadoId, _) = await CreaColaAsync();
        var handler = new ListQueuedEmailsQueryHandler(store);

        // Act
        var enviados = await handler.Handle(new ListQueuedEmailsQuery("sent", 1, 20), CancellationToken.None);
        var segundaPagina = await handler.Handle(new ListQueuedEmailsQuery(null, 2, 2), CancellationToken.None);

        // Assert
        Assert.Equal(enviadoId, Assert.Single(enviados.Items).Id);
        Assert.Equal(3, segundaPagina.TotalItems);
        Assert.Equal("Pending", Assert.Single(segundaPagina.Items).Status);
    }

    [Fact]
    public async Task RF_NOT_11_la_consulta_no_expone_el_cuerpo()
    {
        // Arrange
        var (store, pendienteId, _, _) = await CreaColaAsync();

        // Act
        var lista = await store.ListAsync(null, 1, 20, CancellationToken.None);
        var uno = await store.GetByIdAsync(pendienteId, CancellationToken.None);
        var json = JsonSerializer.Serialize(new { lista, uno }, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        // Assert
        Assert.Null(typeof(QueuedEmailDTO).GetProperty("Body"));
        Assert.DoesNotContain("\"body\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Cuerpo de prueba", json);
    }

    [Fact]
    public async Task RF_NOT_11_un_correo_que_no_existe_responde_404()
    {
        // Arrange
        var (store, _, _, _) = await CreaColaAsync();
        var handler = new GetQueuedEmailQueryHandler(store);

        // Act
        var exception = await Record.ExceptionAsync(
            () => handler.Handle(new GetQueuedEmailQuery(Guid.NewGuid()), CancellationToken.None));

        // Assert
        var notFound = Assert.IsType<NotFoundException>(exception);
        Assert.Equal("notification.email-not-found", notFound.Code);
    }

    [Fact]
    public async Task RF_NOT_11_consulta_un_correo_por_id()
    {
        // Arrange
        var (store, _, enviadoId, _) = await CreaColaAsync();
        var handler = new GetQueuedEmailQueryHandler(store);

        // Act
        var correo = await handler.Handle(new GetQueuedEmailQuery(enviadoId), CancellationToken.None);

        // Assert
        Assert.Equal("Sent", correo.Status);
        Assert.Equal(T0.AddMinutes(2), correo.SentAt);
        Assert.Equal(1, correo.Attempts);
    }

    [Theory]
    [InlineData("Pending", true)]
    [InlineData("failed", true)]
    [InlineData("Borrado", false)]
    [InlineData("1", false)]
    public void RF_NOT_11_el_filtro_de_estado_solo_acepta_los_tres_nombres(string valor, bool valido)
    {
        // Arrange
        var validator = new ListQueuedEmailsQueryValidator();

        // Act
        var result = validator.Validate(new ListQueuedEmailsQuery(valor, 1, 20));

        // Assert
        Assert.Equal(valido, result.IsValid);
    }

    [Fact]
    public void RF_NOT_11_pagina_fuera_de_rango_es_invalida()
    {
        // Arrange
        var validator = new ListQueuedEmailsQueryValidator();

        // Act
        var result = validator.Validate(new ListQueuedEmailsQuery(null, 0, 500));

        // Assert
        Assert.Equal(new[] { "page", "pageSize" }, result.Errors.Select(e => e.PropertyName).OrderBy(p => p));
    }
}

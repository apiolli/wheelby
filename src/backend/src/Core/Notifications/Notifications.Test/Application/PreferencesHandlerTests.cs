using Notifications.Application.Features.Preferences;
using Notifications.Infrastructure.Persistence;
using Notifications.Test.TestSupport;

namespace Notifications.Test.Application;

public sealed class PreferencesHandlerTests
{
    private static readonly DateTime Ahora = new(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid AnaId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid BetoId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003");

    private static GetMyPreferencesQueryHandler Get(NotificationsDbContext db, Guid userId)
        => new(new NotificationPreferenceRepository(db), new FixedCurrentUser(userId));

    private static UpdateMyPreferencesCommandHandler Update(NotificationsDbContext db, Guid userId)
        => new(new NotificationPreferenceRepository(db), new NotificationsUnitOfWork(db),
            new FixedCurrentUser(userId), new FixedClock(Ahora));

    [Fact]
    public async Task RF_NOT_07_preferencia_por_defecto_es_correo_activado()
    {
        // Arrange
        var db = NotificationsDb.CreaContexto();

        // Act
        var preferencia = await Get(db, AnaId).Handle(new GetMyPreferencesQuery(), CancellationToken.None);

        // Assert
        Assert.True(preferencia.EmailEnabled);
    }

    [Fact]
    public async Task RF_NOT_07_desactivar_el_correo_se_guarda_y_se_lee()
    {
        // Arrange
        var db = NotificationsDb.CreaContexto();

        // Act
        await Update(db, AnaId).Handle(new UpdateMyPreferencesCommand(false), CancellationToken.None);
        var preferencia = await Get(db, AnaId).Handle(new GetMyPreferencesQuery(), CancellationToken.None);

        // Assert
        Assert.False(preferencia.EmailEnabled);
        Assert.Equal(1, db.NotificationPreferences.Count());
    }

    [Fact]
    public async Task RF_NOT_07_volver_a_activar_actualiza_la_misma_fila()
    {
        // Arrange
        var db = NotificationsDb.CreaContexto();
        await Update(db, AnaId).Handle(new UpdateMyPreferencesCommand(false), CancellationToken.None);

        // Act
        await Update(db, AnaId).Handle(new UpdateMyPreferencesCommand(true), CancellationToken.None);

        // Assert
        var fila = Assert.Single(db.NotificationPreferences);
        Assert.True(fila.EmailEnabled);
    }

    [Fact]
    public async Task RF_NOT_07_cada_usuario_cambia_solo_su_preferencia()
    {
        // Arrange
        var db = NotificationsDb.CreaContexto();

        // Act
        await Update(db, AnaId).Handle(new UpdateMyPreferencesCommand(false), CancellationToken.None);
        var deBeto = await Get(db, BetoId).Handle(new GetMyPreferencesQuery(), CancellationToken.None);

        // Assert
        Assert.True(deBeto.EmailEnabled);
        Assert.Equal(AnaId, Assert.Single(db.NotificationPreferences).UserId);
    }

    [Fact]
    public void RF_NOT_07_sin_email_enabled_responde_400()
    {
        // Arrange
        var validator = new UpdateMyPreferencesCommandValidator();

        // Act
        var result = validator.Validate(new UpdateMyPreferencesCommand(null));

        // Assert
        Assert.Equal("emailEnabled", Assert.Single(result.Errors).PropertyName);
    }
}

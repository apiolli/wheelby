using AccessControl.Domain.Users;
using AccessControl.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Notifications.Application.Features.PublishNotification;
using Notifications.Domain.Inbox;
using Notifications.Infrastructure.Inbox;
using Notifications.Infrastructure.Persistence;
using Permissions.Application.Abstractions;
using Permissions.Infrastructure.Notifiers;

namespace Permissions.Test.Support;

// RD-12: el motor de notificaciones real (publicador, PublishNotification y directorio
// de usuarios de AccessControl) sobre dos bases EF InMemory, sin PostgreSQL ni la app.
// Sirve para comprobar quién recibe cada aviso de Gestión de permisos.
internal sealed class NotificationEngine
{
    private readonly DateTime _now;

    public AccessControlDbContext Users { get; }
    public NotificationsDbContext Inbox { get; }
    public IPermissionRequestNotifier Notifier { get; }

    public NotificationEngine(DateTime now)
    {
        _now = now;
        Users = new AccessControlDbContext(new DbContextOptionsBuilder<AccessControlDbContext>()
            .UseInMemoryDatabase($"access-control-tests-{Guid.NewGuid()}").Options);
        Inbox = new NotificationsDbContext(new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseInMemoryDatabase($"notifications-tests-{Guid.NewGuid()}").Options);

        var directory = new UserDirectory(Users);
        var publish = new PublishNotificationCommandHandler(
            new NotificationRepository(Inbox),
            new QueuedEmailRepository(Inbox),
            new NotificationPreferenceRepository(Inbox),
            new NotificationsUnitOfWork(Inbox),
            directory,
            new FixedClock(now),
            NullLogger<PublishNotificationCommandHandler>.Instance);
        var publisher = new NotificationPublisher(new PublishOnlySender(publish), directory);

        Notifier = new PermissionRequestNotifier(publisher, directory, NullLogger<PermissionRequestNotifier>.Instance);
    }

    public Guid Administrador(string nombre, string correo)
        => Guardar(User.CreateAdministrator(nombre, Email.Create(correo), "hash", _now));

    public Guid AdministradorDeshabilitado(string nombre, string correo)
    {
        var user = User.CreateAdministrator(nombre, Email.Create(correo), "hash", _now);
        user.Disable(Guid.NewGuid(), _now);
        return Guardar(user);
    }

    public Guid AdministradorSinActivar(string nombre, string correo)
    {
        var user = User.Create(nombre, Email.Create(correo), "hash", _now);
        user.ChangeRole(Role.Administrator);
        return Guardar(user);
    }

    public Guid Estandar(string nombre, string correo)
        => Guardar(User.Create(nombre, Email.Create(correo), "hash", _now));

    public List<Notification> De(Guid userId)
        => Inbox.Notifications.Where(n => n.RecipientId == userId).ToList();

    public List<Notification> DelTipo(string type)
        => Inbox.Notifications.Where(n => n.Type == type).ToList();

    private Guid Guardar(User user)
    {
        Users.Users.Add(user);
        Users.SaveChanges();
        return user.Id;
    }

    // ISender mínimo: solo despacha PublishNotificationCommand a su handler.
    private sealed class PublishOnlySender(PublishNotificationCommandHandler handler) : ISender
    {
        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
            => request is PublishNotificationCommand command
                ? (TResponse)(object)await handler.Handle(command, cancellationToken)
                : throw new NotSupportedException(request.GetType().Name);

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
            => throw new NotSupportedException();

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}

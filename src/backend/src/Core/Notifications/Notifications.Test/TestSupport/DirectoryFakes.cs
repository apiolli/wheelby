using AccessControl.Contracts;
using MediatR;

namespace Notifications.Test.TestSupport;

// Directorio de usuarios de prueba: solo los que la prueba agrega existen (activos).
public sealed class FakeUserDirectory : IUserDirectory
{
    public Dictionary<Guid, UserContact> Users { get; } = [];

    public FakeUserDirectory Con(Guid id, string fullName, string email)
    {
        Users[id] = new UserContact(id, fullName, email);
        return this;
    }

    public Task<UserContact?> GetContactAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult(Users.GetValueOrDefault(userId));
}

// Directorio de Administradores activos con la lista que la prueba decide.
public sealed class FakeAdministratorDirectory(params UserContact[] admins) : IAdministratorDirectory
{
    public Task<IReadOnlyList<UserContact>> ListActiveAdministratorsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<UserContact>>(admins);
}

// ISender que solo registra lo que se le manda.
public sealed class RecordingSender : ISender
{
    public List<object> Requests { get; } = [];

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        return Task.FromResult(default(TResponse)!);
    }

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
    {
        Requests.Add(request);
        return Task.CompletedTask;
    }

    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        return Task.FromResult<object?>(null);
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}

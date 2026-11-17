using AccessControl.Contracts;
using Documents.Contracts;
using Notifications.Contracts;
using Wheelby.Application.Abstractions;
using Wheelby.Domain.Reviews;

namespace Wheelby.Test.TestSupport;

public sealed class FakeDocumentAccess : IDocumentAccess
{
    private readonly Dictionary<Guid, DocumentInfo> _docs = [];

    public Guid Agregar(Guid ownerId, string contentType = "image/jpeg")
    {
        var id = Guid.NewGuid();
        _docs[id] = new DocumentInfo(id, ownerId, "archivo.jpg", contentType, 1234);
        return id;
    }

    public Task<DocumentInfo?> GetInfoAsync(Guid documentId, CancellationToken cancellationToken = default)
        => Task.FromResult(_docs.GetValueOrDefault(documentId));

    public Task<DocumentFile?> OpenReadAsync(Guid documentId, CancellationToken cancellationToken = default)
        => Task.FromResult(_docs.TryGetValue(documentId, out var d)
            ? new DocumentFile(new MemoryStream([1, 2, 3]), d.ContentType, d.FileName)
            : null);
}

public sealed class FakeUserDirectory : IUserDirectory
{
    public Dictionary<Guid, UserContact> Users { get; } = [];

    public void Agregar(Guid id, string email) => Users[id] = new UserContact(id, "Usuario " + email, email);

    public Task<UserContact?> GetContactAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult(Users.GetValueOrDefault(userId));
}

public sealed class FakeEmailQueue : IEmailQueue
{
    public List<(string To, string Subject, string Body)> Sent { get; } = [];
    public Exception? FailWith { get; set; }

    public Task EnqueueAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (FailWith is not null) throw FailWith;
        Sent.Add((to, subject, body));
        return Task.CompletedTask;
    }
}

public sealed class InMemoryReviewRepository : IReviewRepository
{
    public List<Review> Reviews { get; } = [];

    public Task<bool> ExistsAsync(Guid reservationId, Guid authorId, CancellationToken cancellationToken = default)
        => Task.FromResult(Reviews.Any(r => r.ReservationId == reservationId && r.AuthorId == authorId));

    public Task AddAsync(Review review, CancellationToken cancellationToken = default)
    {
        Reviews.Add(review);
        return Task.CompletedTask;
    }
}

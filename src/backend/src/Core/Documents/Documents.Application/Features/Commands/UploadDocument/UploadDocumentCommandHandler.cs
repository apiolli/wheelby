using AccessControl.Contracts;
using Documents.Application.Abstractions;
using Documents.Domain.Documents;
using MediatR;
using Shared.Application.Abstractions;

namespace Documents.Application.Features.UploadDocument;

internal sealed class UploadDocumentCommandHandler(
    IDocumentRepository documents,
    IFileStorage storage,
    IDocumentsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<UploadDocumentCommand, Guid>
{
    public async Task<Guid> Handle(UploadDocumentCommand command, CancellationToken cancellationToken)
    {
        var document = Document.Create(
            currentUser.Id,
            command.FileName,
            command.ContentType,
            command.Length,
            clock.UtcNow);

        // Primero el archivo y después el registro: nunca queda una fila sin su archivo.
        await storage.SaveAsync(document.StoredFileName, command.Content, cancellationToken);

        try
        {
            await documents.AddAsync(document, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Compensación: si el registro no se guarda, el archivo no debe quedar huérfano.
            await storage.DeleteAsync(document.StoredFileName, CancellationToken.None);
            throw;
        }

        return document.Id;
    }
}

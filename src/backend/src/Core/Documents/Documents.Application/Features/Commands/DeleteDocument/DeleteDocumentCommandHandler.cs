using AccessControl.Contracts;
using Documents.Application.Abstractions;
using Documents.Domain.Documents.Exceptions;
using MediatR;
using Shared.Application.Abstractions;
using SharedKernel.Exceptions;

namespace Documents.Application.Features.DeleteDocument;

// RF-DOC-04: eliminación lógica. No borra la fila ni toca el archivo.
internal sealed class DeleteDocumentCommandHandler(
    IDocumentRepository documents,
    IDocumentsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<DeleteDocumentCommand, Unit>
{
    public async Task<Unit> Handle(DeleteDocumentCommand command, CancellationToken cancellationToken)
    {
        // Precedencia: primero 404 (no existe o ya está eliminado) y después 403 (ajeno).
        var document = await documents.GetByIdAsync(command.Id, cancellationToken);
        if (document is null || document.IsDeleted)
            throw new DocumentNotFoundException();

        var isAdministrator = currentUser.Role == AccessControlRoles.Administrator;
        if (!document.CanBeAccessedBy(currentUser.Id, isAdministrator))
            throw new ForbiddenException("No tienes permiso para acceder a este documento.");

        document.MarkDeleted(currentUser.Id, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

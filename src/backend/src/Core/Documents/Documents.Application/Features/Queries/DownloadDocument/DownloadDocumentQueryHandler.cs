using AccessControl.Contracts;
using Documents.Application.Abstractions;
using Documents.Domain.Documents.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedKernel.Exceptions;

namespace Documents.Application.Features.DownloadDocument;

internal sealed class DownloadDocumentQueryHandler(
    IDocumentRepository documents,
    IFileStorage storage,
    ICurrentUser currentUser,
    ILogger<DownloadDocumentQueryHandler> logger) : IRequestHandler<DownloadDocumentQuery, DocumentContent>
{
    public async Task<DocumentContent> Handle(DownloadDocumentQuery request, CancellationToken cancellationToken)
    {
        // Precedencia: primero 404 (no existe o está eliminado) y después 403 (ajeno).
        // Así un eliminado se comporta igual para todos y no revela de quién era.
        var document = await documents.GetByIdAsync(request.Id, cancellationToken);
        if (document is null || document.IsDeleted)
            throw new DocumentNotFoundException();

        var isAdministrator = currentUser.Role == AccessControlRoles.Administrator;
        if (!document.CanBeAccessedBy(currentUser.Id, isAdministrator))
            throw new ForbiddenException("No tienes permiso para acceder a este documento.");

        var content = await storage.OpenReadAsync(document.StoredFileName, cancellationToken);
        if (content is null)
        {
            logger.LogError(
                "El archivo del documento {DocumentId} no está en el almacenamiento.", document.Id);
            throw new NotFoundException("El archivo del documento no está disponible.");
        }

        return new DocumentContent(content, document.ContentType, document.FileName);
    }
}

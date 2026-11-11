using Documents.Application.Features.DeleteDocument;
using Documents.Application.Features.DownloadDocument;
using Documents.Application.Features.ListDocuments;
using Documents.Application.Features.UploadDocument;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SharedKernel.Exceptions;

namespace Documents.Infrastructure;

public static class DocumentsEndpoints
{
    // RD-06: cualquier usuario con sesión válida. Quién puede ver o eliminar cada
    // documento lo decide el servidor con Document.CanBeAccessedBy.
    public static IEndpointRouteBuilder MapDocumentsEndpoints(this IEndpointRouteBuilder app)
    {
        var documents = app.MapGroup("/documents")
            .WithTags("Documents")
            .RequireAuthorization();

        documents.MapPost("", async (
            IFormFile? file,
            [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            if (file is null)
                throw new BadRequestException("Debes adjuntar un archivo en el campo file.");

            await using var content = file.OpenReadStream();
            var id = await sender.Send(
                new UploadDocumentCommand(file.FileName, file.ContentType, file.Length, content),
                ct);

            return Results.Json(new { id }, statusCode: StatusCodes.Status201Created);
        })
        // La API usa Bearer y no cookies; sin esto, un endpoint con IFormFile falla al ejecutarse.
        .DisableAntiforgery();

        documents.MapGet("", async (
            int? page,
            int? pageSize,
            [FromServices] ISender sender,
            CancellationToken ct)
            => Results.Ok(await sender.Send(
                new ListDocumentsQuery(page ?? 1, pageSize ?? 20),
                ct)));

        documents.MapGet("/{id:guid}/content", async (
            Guid id,
            HttpContext http,
            [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            var document = await sender.Send(new DownloadDocumentQuery(id), ct);

            // Siempre como adjunto, y el navegador no adivina el tipo.
            http.Response.Headers["X-Content-Type-Options"] = "nosniff";
            return Results.File(
                document.Content,
                document.ContentType,
                fileDownloadName: document.FileName);
        });

        documents.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new DeleteDocumentCommand(id), ct);
            return Results.NoContent();
        });

        return app;
    }
}

using Shared.Application.Abstractions;

namespace Documents.Application.Features.UploadDocument;

public sealed record UploadDocumentCommand(
    string FileName,
    string? ContentType,
    long Length,
    Stream Content) : ICommand<Guid>;

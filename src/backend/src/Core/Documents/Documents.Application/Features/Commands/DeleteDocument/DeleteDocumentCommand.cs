using Shared.Application.Abstractions;

namespace Documents.Application.Features.DeleteDocument;

public sealed record DeleteDocumentCommand(Guid Id) : ICommand;

using Shared.Application.Abstractions;

namespace Documents.Application.Features.DownloadDocument;

public sealed record DownloadDocumentQuery(Guid Id) : IQuery<DocumentContent>;

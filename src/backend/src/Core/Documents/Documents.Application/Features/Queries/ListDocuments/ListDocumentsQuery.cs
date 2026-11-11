using Documents.Application.Common;
using Documents.Application.DTOs;
using Shared.Application.Abstractions;

namespace Documents.Application.Features.ListDocuments;

public sealed record ListDocumentsQuery(int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<DocumentSummaryDTO>>;

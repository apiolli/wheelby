namespace Documents.Application.Features.DownloadDocument;

public sealed record DocumentContent(Stream Content, string ContentType, string FileName);

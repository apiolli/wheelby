using FluentValidation;
using MediatR;
using Notifications.Application.Abstractions;
using Notifications.Application.Common;
using Notifications.Application.DTOs;
using Notifications.Domain.Emails.Enums;
using Shared.Application.Abstractions;

namespace Notifications.Application.Features.ListQueuedEmails;

// GET /admin/notifications/emails?status=&page=&pageSize= (RF-NOT-11).
public sealed record ListQueuedEmailsQuery(string? Status, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<QueuedEmailDTO>>;

internal sealed class ListQueuedEmailsQueryValidator : AbstractValidator<ListQueuedEmailsQuery>
{
    public ListQueuedEmailsQueryValidator()
    {
        RuleFor(x => x.Status)
            .Must(s => s is null || ListQueuedEmailsQueryHandler.TryParseStatus(s, out _))
            .WithMessage("El estado debe ser Pending, Sent o Failed.");

        this.AddPagingRules(x => x.Page, x => x.PageSize);
    }
}

internal sealed class ListQueuedEmailsQueryHandler(IQueuedEmailReadStore reader)
    : IRequestHandler<ListQueuedEmailsQuery, PagedResult<QueuedEmailDTO>>
{
    public Task<PagedResult<QueuedEmailDTO>> Handle(ListQueuedEmailsQuery request, CancellationToken cancellationToken)
    {
        EmailStatus? status = request.Status is not null && TryParseStatus(request.Status, out var parsed)
            ? parsed
            : null;

        return reader.ListAsync(status, request.Page, request.PageSize, cancellationToken);
    }

    // Solo nombres (Pending, Sent, Failed), sin distinguir mayúsculas; "1" no vale.
    internal static bool TryParseStatus(string value, out EmailStatus status)
        => Enum.TryParse(value, ignoreCase: true, out status)
           && Enum.IsDefined(status)
           && !int.TryParse(value, out _);
}

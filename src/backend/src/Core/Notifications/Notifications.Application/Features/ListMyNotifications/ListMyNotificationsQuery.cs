using AccessControl.Contracts;
using FluentValidation;
using MediatR;
using Notifications.Application.Abstractions;
using Notifications.Application.Common;
using Notifications.Application.DTOs;
using Shared.Application.Abstractions;

namespace Notifications.Application.Features.ListMyNotifications;

// GET /notifications?read=&page=&pageSize= (RF-NOT-04). Solo las del usuario de la sesión.
public sealed record ListMyNotificationsQuery(bool? Read, int Page = 1, int PageSize = 20)
    : IQuery<NotificationPageDTO>;

internal sealed class ListMyNotificationsQueryValidator : AbstractValidator<ListMyNotificationsQuery>
{
    public ListMyNotificationsQueryValidator() => this.AddPagingRules(x => x.Page, x => x.PageSize);
}

internal sealed class ListMyNotificationsQueryHandler(
    INotificationReadStore reader,
    ICurrentUser currentUser) : IRequestHandler<ListMyNotificationsQuery, NotificationPageDTO>
{
    public Task<NotificationPageDTO> Handle(ListMyNotificationsQuery request, CancellationToken cancellationToken)
        => reader.ListForRecipientAsync(currentUser.Id, request.Read, request.Page, request.PageSize, cancellationToken);
}

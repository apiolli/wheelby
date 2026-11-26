using AccessControl.Contracts;
using MediatR;
using Notifications.Application.Abstractions;
using Notifications.Application.Common;
using Shared.Application.Abstractions;
using SharedKernel.Exceptions;

namespace Notifications.Application.Features.MarkNotificationAsRead;

// POST /notifications/{id}/read (RF-NOT-05).
public sealed record MarkNotificationAsReadCommand(Guid Id) : ICommand;

internal sealed class MarkNotificationAsReadCommandHandler(
    INotificationRepository notifications,
    INotificationsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<MarkNotificationAsReadCommand, Unit>
{
    public async Task<Unit> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        // CA-NOT-05.2: la de otro usuario responde lo mismo que una que no existe (404),
        // para no revelar que existe, y no se toca.
        var notification = await notifications.GetForRecipientAsync(request.Id, currentUser.Id, cancellationToken)
            ?? throw new NotFoundException("La notificación no existe.", NotificationErrorCodes.NotificationNotFound);

        if (notification.IsRead)
            return Unit.Value;

        notification.MarkAsRead(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

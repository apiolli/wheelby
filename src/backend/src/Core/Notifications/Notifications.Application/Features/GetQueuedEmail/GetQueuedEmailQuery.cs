using MediatR;
using Notifications.Application.Abstractions;
using Notifications.Application.Common;
using Notifications.Application.DTOs;
using Shared.Application.Abstractions;
using SharedKernel.Exceptions;

namespace Notifications.Application.Features.GetQueuedEmail;

// GET /admin/notifications/emails/{id} (RF-NOT-11). 404 si no existe.
public sealed record GetQueuedEmailQuery(Guid Id) : IQuery<QueuedEmailDTO>;

internal sealed class GetQueuedEmailQueryHandler(IQueuedEmailReadStore reader)
    : IRequestHandler<GetQueuedEmailQuery, QueuedEmailDTO>
{
    public async Task<QueuedEmailDTO> Handle(GetQueuedEmailQuery request, CancellationToken cancellationToken)
        => await reader.GetByIdAsync(request.Id, cancellationToken)
           ?? throw new NotFoundException("El correo no existe.", NotificationErrorCodes.EmailNotFound);
}

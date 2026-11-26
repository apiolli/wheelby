using AccessControl.Contracts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Notifications.Application.Features.GetQueuedEmail;
using Notifications.Application.Features.ListQueuedEmails;

namespace Notifications.Infrastructure;

public static class NotificationsEndpoints
{
    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        MapAdminNotificationEndpoints(app);
        return app;
    }

    // RF-NOT-11: la cola solo la ve el Administrador. La política se declara una vez,
    // sobre el grupo (RD-06).
    private static void MapAdminNotificationEndpoints(IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/admin/notifications")
            .WithTags("Admin: notifications")
            .RequireAuthorization(AccessControlPolicies.RequireAdministrator);

        admin.MapGet("/emails", async (
            string? status,
            int? page,
            int? pageSize,
            [FromServices] ISender sender,
            CancellationToken ct)
            => Results.Ok(await sender.Send(
                new ListQueuedEmailsQuery(status, page ?? 1, pageSize ?? 20), ct)));

        admin.MapGet("/emails/{id:guid}", async (Guid id, [FromServices] ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new GetQueuedEmailQuery(id), ct)));
    }
}

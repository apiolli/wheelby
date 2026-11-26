using AccessControl.Contracts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Notifications.Application.Features.GetQueuedEmail;
using Notifications.Application.Features.ListMyNotifications;
using Notifications.Application.Features.ListQueuedEmails;
using Notifications.Application.Features.MarkNotificationAsRead;
using Notifications.Application.Features.Preferences;

namespace Notifications.Infrastructure;

public static class NotificationsEndpoints
{
    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        MapInboxEndpoints(app);
        MapAdminNotificationEndpoints(app);
        return app;
    }

    // RF-NOT-04 a 07: la bandeja y la preferencia del usuario de la sesión (los dos roles).
    // Ninguna ruta recibe un userId: el dueño siempre es ICurrentUser.
    private static void MapInboxEndpoints(IEndpointRouteBuilder app)
    {
        var inbox = app.MapGroup("/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        inbox.MapGet("/", async (
            bool? read,
            int? page,
            int? pageSize,
            [FromServices] ISender sender,
            CancellationToken ct)
            => Results.Ok(await sender.Send(
                new ListMyNotificationsQuery(read, page ?? 1, pageSize ?? 20), ct)));

        inbox.MapPost("/{id:guid}/read", async (Guid id, [FromServices] ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new MarkNotificationAsReadCommand(id), ct);
            return Results.NoContent();
        });

        inbox.MapGet("/preferences", async ([FromServices] ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new GetMyPreferencesQuery(), ct)));

        inbox.MapPut("/preferences", async (
            UpdatePreferencesRequest request,
            [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new UpdateMyPreferencesCommand(request.EmailEnabled), ct);
            return Results.NoContent();
        });
    }

    private sealed record UpdatePreferencesRequest(bool? EmailEnabled);

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

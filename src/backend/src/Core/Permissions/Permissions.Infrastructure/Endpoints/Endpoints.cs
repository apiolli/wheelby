using AccessControl.Contracts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Permissions.Application.Features.ApprovePermissionRequest;
using Permissions.Application.Features.CreatePermissionRequest;
using Permissions.Application.Features.ExecuteDemoAction;
using Permissions.Application.Features.ListMyRequests;
using Permissions.Application.Features.ListPendingRequests;
using Permissions.Application.Features.RejectPermissionRequest;

namespace Permissions.Infrastructure;

public static class PermissionsEndpoints
{
    public static IEndpointRouteBuilder MapPermissionsEndpoints(this IEndpointRouteBuilder app)
    {
        MapPermissionEndpoints(app);
        MapAdminPermissionEndpoints(app);
        return app;
    }

    private static void MapPermissionEndpoints(IEndpointRouteBuilder app)
    {
        var permissions = app.MapGroup("/permissions")
            .WithTags("Permissions")
            .RequireAuthorization();

        permissions.MapPost("/requests", async (
            CreatePermissionRequestRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var id = await sender.Send(
                new CreatePermissionRequestCommand(request.PermissionCode),
                ct);

            return Results.Json(new { id }, statusCode: StatusCodes.Status201Created);
        });

        permissions.MapGet("/requests/mine", async (
            int? page,
            int? pageSize,
            ISender sender,
            CancellationToken ct)
            => Results.Ok(await sender.Send(
                new ListMyRequestsQuery(page ?? 1, pageSize ?? 20),
                ct)));

        permissions.MapPost("/demo-action", async (ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ExecuteDemoActionCommand(), ct)));
    }

    // La exigencia de rol se declara una sola vez, sobre el grupo de rutas
    // de administración (RD-06). Ningún handler vuelve a comprobar el rol.
    private static void MapAdminPermissionEndpoints(IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/admin/permissions")
            .WithTags("Admin: permissions")
            .RequireAuthorization(AccessControlPolicies.RequireAdministrator);

        admin.MapGet("/requests", async (
            int? page,
            int? pageSize,
            ISender sender,
            CancellationToken ct)
            => Results.Ok(await sender.Send(
                new ListPendingRequestsQuery(page ?? 1, pageSize ?? 20),
                ct)));

        admin.MapPost("/requests/{id:guid}/approve", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new ApprovePermissionRequestCommand(id), ct);
            return Results.NoContent();
        });

        admin.MapPost("/requests/{id:guid}/reject", async (
            Guid id,
            RejectPermissionRequestRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new RejectPermissionRequestCommand(id, request.Reason), ct);
            return Results.NoContent();
        });
    }

    private sealed record CreatePermissionRequestRequest(string PermissionCode);
    private sealed record RejectPermissionRequestRequest(string? Reason);
}

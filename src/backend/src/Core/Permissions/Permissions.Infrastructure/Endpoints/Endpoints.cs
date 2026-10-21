using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Permissions.Application.Features.CreatePermissionRequest;
using Permissions.Application.Features.ExecuteDemoAction;
using Permissions.Application.Features.ListMyRequests;

namespace Permissions.Infrastructure;

public static class PermissionsEndpoints
{
    public static IEndpointRouteBuilder MapPermissionsEndpoints(this IEndpointRouteBuilder app)
    {
        MapPermissionEndpoints(app);
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

    private sealed record CreatePermissionRequestRequest(string PermissionCode);
}

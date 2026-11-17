using AccessControl.Contracts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wheelby.Application.Features.CancelReservation;
using Wheelby.Application.Features.CompleteRental;
using Wheelby.Application.Features.ConfirmReservation;
using Wheelby.Application.Features.GetReservation;
using Wheelby.Application.Features.GetVehicle;
using Wheelby.Application.Features.GetVehicleAvailability;
using Wheelby.Application.Features.ListMyVehicles;
using Wheelby.Application.Features.ListAllReservations;
using Wheelby.Application.Features.ListMyReservations;
using Wheelby.Application.Features.RejectReservation;
using Wheelby.Application.Features.RequestReservation;
using Wheelby.Application.Features.SearchAvailableVehicles;
using Wheelby.Application.Features.StartRental;
using Wheelby.Application.Features.PublishVehicle;
using Wheelby.Domain.Reservations;

namespace Wheelby.Infrastructure;

// La exigencia de rol se declara una vez por grupo, nunca en el handler.
public static class WheelbyEndpoints
{
    public static IEndpointRouteBuilder MapWheelbyEndpoints(this IEndpointRouteBuilder app)
    {
        MapMemberEndpoints(app);
        MapAdminEndpoints(app);
        return app;
    }

    private static void MapMemberEndpoints(IEndpointRouteBuilder app)
    {
        var vehicles = app.MapGroup("/vehicles")
            .WithTags("Vehicles")
            .RequireAuthorization()
            .RequireAuthorization(AccessControlPolicies.RequireStandard);

        vehicles.MapPost("", async (
            PublishVehicleCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var id = await sender.Send(command, ct);
            return Results.Json(new { id }, statusCode: StatusCodes.Status201Created);
        });

        vehicles.MapGet("/available", async (
            DateTimeOffset from,
            DateTimeOffset to,
            int? page,
            int? pageSize,
            string? currency,
            ISender sender,
            CancellationToken ct) =>
        {
            // Precios en DOP; currency (opcional) pide además la conversión.
            var query = new SearchAvailableVehiclesQuery(
                from.UtcDateTime,
                to.UtcDateTime,
                page ?? 1,
                pageSize ?? 20,
                string.IsNullOrWhiteSpace(currency) ? null : currency.Trim().ToUpperInvariant());
            return Results.Ok(await sender.Send(query, ct));
        });

        vehicles.MapGet("/mine", async (
            int? page,
            int? pageSize,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new ListMyVehiclesQuery(page ?? 1, pageSize ?? 20);
            return Results.Ok(await sender.Send(query, ct));
        });

        vehicles.MapGet("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetVehicleQuery(id), ct)));

        vehicles.MapGet("/{id:guid}/availability", async (
            Guid id,
            DateTimeOffset from,
            DateTimeOffset to,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new GetVehicleAvailabilityQuery(id, from.UtcDateTime, to.UtcDateTime);
            return Results.Ok(await sender.Send(query, ct));
        });

        var reservations = app.MapGroup("/reservations")
            .WithTags("Reservations")
            .RequireAuthorization()
            .RequireAuthorization(AccessControlPolicies.RequireStandard);

        reservations.MapPost("", async (
            RequestReservationRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var command = new RequestReservationCommand(
                request.VehicleId,
                request.Start.UtcDateTime,
                request.End.UtcDateTime);
            var id = await sender.Send(command, ct);
            return Results.Json(new { id }, statusCode: StatusCodes.Status201Created);
        });

        reservations.MapGet("/mine", async (
            string @as,
            int? page,
            int? pageSize,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new ListMyReservationsQuery(@as, page ?? 1, pageSize ?? 20);
            return Results.Ok(await sender.Send(query, ct));
        });

        // Inquilino o dueño del vehículo; para cualquier otro, 404.
        reservations.MapGet("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetReservationQuery(id), ct)));

        reservations.MapPost("/{id:guid}/confirm", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new ConfirmReservationCommand(id), ct);
            return Results.NoContent();
        });

        reservations.MapPost("/{id:guid}/reject", async (
            Guid id,
            RejectReservationCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var resolved = command with { Id = id };
            await sender.Send(resolved, ct);
            return Results.NoContent();
        });

        reservations.MapPost("/{id:guid}/start", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new StartRentalCommand(id), ct);
            return Results.NoContent();
        });

        reservations.MapPost("/{id:guid}/complete", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new CompleteRentalCommand(id), ct);
            return Results.NoContent();
        });

        reservations.MapPost("/{id:guid}/cancel", async (
            Guid id,
            CancelReservationCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var resolved = command with { Id = id };
            await sender.Send(resolved, ct);
            return Results.NoContent();
        });
    }

    private static void MapAdminEndpoints(IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/admin/reservations")
            .WithTags("Admin: reservations")
            .RequireAuthorization(AccessControlPolicies.RequireAdministrator);

        admin.MapGet("", async (
            ReservationStatus? status,
            int? page,
            int? pageSize,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new ListAllReservationsQuery(status, page ?? 1, pageSize ?? 20);
            return Results.Ok(await sender.Send(query, ct));
        });

        // El Administrador no entra al grupo /reservations (RequireStandard): ve el detalle aquí.
        admin.MapGet("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetReservationQuery(id), ct)));
    }

    private sealed record RequestReservationRequest(Guid VehicleId, DateTimeOffset Start, DateTimeOffset End);
}

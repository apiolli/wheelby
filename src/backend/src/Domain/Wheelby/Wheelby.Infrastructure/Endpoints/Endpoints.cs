using AccessControl.Contracts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Wheelby.Application.DTOs;
using Wheelby.Application.Features.AddVehiclePhoto;
using Wheelby.Application.Features.AttachReservationDocument;
using Wheelby.Application.Features.CancelReservation;
using Wheelby.Application.Features.CreateReview;
using Wheelby.Application.Features.GetReservationDocumentContent;
using Wheelby.Application.Features.GetReservationHistory;
using Wheelby.Application.Features.GetVehiclePhotoContent;
using Wheelby.Application.Features.ListReservationDocuments;
using Wheelby.Application.Features.ListReservationReviews;
using Wheelby.Application.Features.ListVehiclePhotos;
using Wheelby.Application.Features.ListVehicleReviews;
using Wheelby.Application.Features.RemoveVehiclePhoto;
using Wheelby.Application.Features.SetVehicleActive;
using Wheelby.Application.Features.UpdateVehicle;
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
using Wheelby.Application.Features.VehicleRevenueReport;
using Wheelby.Domain.Reservations;
using Wheelby.Domain.Vehicles;

namespace Wheelby.Infrastructure;

// La exigencia de rol se declara una vez por grupo, nunca en el handler.
public static class WheelbyEndpoints
{
    public static IEndpointRouteBuilder MapWheelbyEndpoints(this IEndpointRouteBuilder app)
    {
        MapMemberEndpoints(app);
        MapAdminEndpoints(app);
        MapReportEndpoints(app);
        return app;
    }

    // RF-REP-02: los dos roles entran (el Administrador también, aunque no use /vehicles).
    // El handler limita al Estándar a sus vehículos (RF-REP-04).
    private static void MapReportEndpoints(IEndpointRouteBuilder app)
    {
        var reports = app.MapGroup("/reports")
            .WithTags("Reports")
            .RequireAuthorization();

        reports.MapGet("/vehicle-revenue", async (
            string? from,
            string? to,
            [FromServices] ISender sender,
            CancellationToken ct)
            => Results.Ok(await sender.Send(new VehicleRevenueReportQuery(from, to), ct)));
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
            string? city,
            VehicleType? type,
            ISender sender,
            CancellationToken ct) =>
        {
            // Precios en DOP; currency (opcional) pide además la conversión.
            // city (sin distinguir mayúsculas) y type filtran el resultado.
            var query = new SearchAvailableVehiclesQuery(
                from.UtcDateTime,
                to.UtcDateTime,
                page ?? 1,
                pageSize ?? 20,
                string.IsNullOrWhiteSpace(currency) ? null : currency.Trim().ToUpperInvariant(),
                city,
                type);
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

        vehicles.MapPut("/{id:guid}", async (
            Guid id,
            UpdateVehicleRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new UpdateVehicleCommand(
                id, request.Brand, request.Model, request.Year, request.Plate, request.PricePerDay,
                request.Type, request.City, request.Description), ct);
            return Results.NoContent();
        });

        vehicles.MapPost("/{id:guid}/deactivate", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new SetVehicleActiveCommand(id, false), ct);
            return Results.NoContent();
        });

        vehicles.MapPost("/{id:guid}/activate", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new SetVehicleActiveCommand(id, true), ct);
            return Results.NoContent();
        });

        vehicles.MapGet("/{id:guid}/photos", async (Guid id, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new ListVehiclePhotosQuery(id), ct)));

        vehicles.MapPost("/{id:guid}/photos", async (
            Guid id,
            DocumentRefRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new AddVehiclePhotoCommand(id, request.DocumentId), ct);
            return Results.NoContent();
        });

        vehicles.MapDelete("/{id:guid}/photos/{documentId:guid}", async (
            Guid id,
            Guid documentId,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new RemoveVehiclePhotoCommand(id, documentId), ct);
            return Results.NoContent();
        });

        // Cualquier usuario con sesión ve las fotos de un vehículo (se muestran en línea).
        vehicles.MapGet("/{id:guid}/photos/{documentId:guid}", async (
            Guid id,
            Guid documentId,
            ISender sender,
            CancellationToken ct) =>
            File(await sender.Send(new GetVehiclePhotoContentQuery(id, documentId), ct), inline: true));

        vehicles.MapGet("/{id:guid}/reviews", async (
            Guid id,
            int? page,
            int? pageSize,
            ISender sender,
            CancellationToken ct) =>
            Results.Ok(await sender.Send(new ListVehicleReviewsQuery(id, page ?? 1, pageSize ?? 20), ct)));

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

        MapReservationReadRoutes(reservations);

        reservations.MapPost("/{id:guid}/documents", async (
            Guid id,
            AttachDocumentRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new AttachReservationDocumentCommand(id, request.DocumentId, request.Kind), ct);
            return Results.NoContent();
        });

        reservations.MapPost("/{id:guid}/reviews", async (
            Guid id,
            CreateReviewRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var reviewId = await sender.Send(new CreateReviewCommand(id, request.Rating, request.Comment), ct);
            return Results.Json(new { id = reviewId }, statusCode: StatusCodes.Status201Created);
        });

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

        MapReservationReadRoutes(admin);
    }

    // Lecturas de lo que cuelga de una reserva. Los handlers aplican la misma regla
    // de visibilidad que el detalle (participantes o Administrador; otro → 404).
    private static void MapReservationReadRoutes(RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}/history", async (Guid id, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetReservationHistoryQuery(id), ct)));

        group.MapGet("/{id:guid}/documents", async (Guid id, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new ListReservationDocumentsQuery(id), ct)));

        group.MapGet("/{id:guid}/documents/{documentId:guid}/content", async (
            Guid id,
            Guid documentId,
            ISender sender,
            CancellationToken ct) =>
            File(await sender.Send(new GetReservationDocumentContentQuery(id, documentId), ct), inline: false));

        group.MapGet("/{id:guid}/reviews", async (Guid id, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new ListReservationReviewsQuery(id), ct)));
    }

    private static IResult File(FileContentDTO file, bool inline)
        => Results.File(file.Content, file.ContentType, inline ? null : file.FileName, enableRangeProcessing: false);

    private sealed record RequestReservationRequest(Guid VehicleId, DateTimeOffset Start, DateTimeOffset End);

    private sealed record UpdateVehicleRequest(
        string Brand,
        string Model,
        int Year,
        string Plate,
        decimal PricePerDay,
        VehicleType Type,
        string? City,
        string? Description);

    private sealed record DocumentRefRequest(Guid DocumentId);

    private sealed record AttachDocumentRequest(Guid DocumentId, ReservationDocumentKind Kind);

    private sealed record CreateReviewRequest(int Rating, string? Comment);
}

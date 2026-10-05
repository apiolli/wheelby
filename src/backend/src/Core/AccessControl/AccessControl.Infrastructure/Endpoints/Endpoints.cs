using AccessControl.Application.Features.Activate;
using AccessControl.Application.Features.GetCurrentUser;
using AccessControl.Application.Features.Logout;
using AccessControl.Application.Features.Refresh;
using AccessControl.Application.Features.Register;
using AccessControl.Application.Features.ResendActivation;
using Accessontrol.Application.Features.Login;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AccessControl.Infrastructure;

public static class AccessControlEndpoints
{
    public static IEndpointRouteBuilder MapAccessControlEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/auth").WithTags("Auth");

        auth.MapPost("/register", async (RegisterRequest request, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new RegisterUserCommand(request.FullName, request.Email, request.Password), ct);
            return Results.Json(
                new { message = "Cuenta creada. Revisa tu correo para activarla." },
                statusCode: StatusCodes.Status201Created);
        });

        // Get temporal
        auth.MapGet("/activate", async (Guid userId, string token, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new ActivateAccountCommand(userId, token), ct);
            return Results.Ok(new { message = "Cuenta activada. Ya puedes iniciar sesión." });
        });
        
        // Post para el frontend
        auth.MapPost("/activate", async (ActivateRequest request, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new ActivateAccountCommand(request.UserId, request.Token), ct);
            return Results.Ok(new { message = "Cuenta activada. Ya puedes iniciar sesión." });
        });

        auth.MapPost("/resend-activation", async (ResendRequest request, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new ResendActivationCommand(request.Email), ct);
            // Respuesta idéntica exista o no el correo
            return Results.Ok(new
            {
                message = "Si el correo corresponde a una cuenta pendiente de activar, recibirás un nuevo enlace."
            });
        });

        auth.MapPost("/login", async (LoginRequest request, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new LoginCommand(request.Email, request.Password), ct)));

        auth.MapPost("/logout", async (ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new LogoutCommand(), ct);
            return Results.NoContent();

        }).RequireAuthorization();

        auth.MapGet("/me", async (ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new GetCurrentUserQuery(), ct)))
            .RequireAuthorization();

        auth.MapPost("/refresh", async (ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new RefreshSessionCommand(), ct)))
            .RequireAuthorization();    

        return app;
    }

    private sealed record RegisterRequest(string FullName, string Email, string Password);
    private sealed record ActivateRequest(Guid UserId, string Token);
    private sealed record ResendRequest(string Email);
    private sealed record LoginRequest(string Email, string Password);
}
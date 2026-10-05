using AccessControl.Application.Features.Activate;
using AccessControl.Application.Features.ChangePassword;
using AccessControl.Application.Features.ChangeRole;
using AccessControl.Application.Features.DisableUser;
using AccessControl.Application.Features.EnableUser;
using AccessControl.Application.Features.ForcePasswordReset;
using AccessControl.Application.Features.GetCurrentUser;
using AccessControl.Application.Features.ListUsers;
using AccessControl.Application.Features.Logout;
using AccessControl.Application.Features.Refresh;
using AccessControl.Application.Features.Register;
using AccessControl.Application.Features.RequestPasswordReset;
using AccessControl.Application.Features.ResendActivation;
using AccessControl.Application.Features.ResetPassword;
using AccessControl.Contracts;
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
        MapAuthEndpoints(app);
        MapAdminUserEndpoints(app);
        return app;
    }

    private static void MapAuthEndpoints(IEndpointRouteBuilder app)
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

        // POST para el frontend
        auth.MapPost("/activate", async (ActivateRequest request, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new ActivateAccountCommand(request.UserId, request.Token), ct);
            return Results.Ok(new { message = "Cuenta activada. Ya puedes iniciar sesión." });
        });

        auth.MapPost("/resend-activation", async (ResendRequest request, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new ResendActivationCommand(request.Email), ct);
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

        auth.MapPost("/forgot-password", async (ForgotPasswordRequest request, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new RequestPasswordResetCommand(request.Email), ct);
            return Results.Ok(new
            {
                message = "Si el correo corresponde a una cuenta activa, recibirás un código para restablecer tu contraseña."
            });
        });

        auth.MapPost("/reset-password", async (ResetPasswordRequest request, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new ResetPasswordCommand(request.Email, request.Code, request.NewPassword), ct);
            return Results.Ok(new { message = "Contraseña restablecida. Ya puedes iniciar sesión." });
        });

        auth.MapPost("/change-password", async (ChangePasswordRequest request, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ChangePasswordCommand(request.CurrentPassword, request.NewPassword), ct)))
            .RequireAuthorization();
    }

    private static void MapAdminUserEndpoints(IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/admin/users")
            .WithTags("Admin: users")
            .RequireAuthorization(AccessControlPolicies.RequireAdministrator);

        admin.MapGet("", async (int? page, int? pageSize, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ListUsersQuery(page ?? 1, pageSize ?? 20), ct)));

        admin.MapPatch("/{id:guid}/role", async (Guid id, ChangeRoleRequest request, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new ChangeUserRoleCommand(id, request.Role), ct);
            return Results.NoContent();
        });

        admin.MapPost("/{id:guid}/disable", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new DisableUserCommand(id), ct);
            return Results.NoContent();
        });

        admin.MapPost("/{id:guid}/enable", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new EnableUserCommand(id), ct);
            return Results.NoContent();
        });

        admin.MapPost("/{id:guid}/force-password-reset", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new ForcePasswordResetCommand(id), ct);
            return Results.NoContent();
        });
    }

    private sealed record RegisterRequest(string FullName, string Email, string Password);
    private sealed record ActivateRequest(Guid UserId, string Token);
    private sealed record ResendRequest(string Email);
    private sealed record LoginRequest(string Email, string Password);
    private sealed record ChangeRoleRequest(string Role);
    private sealed record ForgotPasswordRequest(string Email);
    private sealed record ResetPasswordRequest(string Email, string Code, string NewPassword);
    private sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
}
using AccessControl.Application.Abstractions;
using AccessControl.Domain.Users;
using MediatR;
using Shared.Application.Abstractions;

namespace AccessControl.Application.Features.EnsureAdministrator;

// Lo ejecuta el arranque de la API (seed), no un endpoint

internal sealed class EnsureAdministratorCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IClock clock) : IRequestHandler<EnsureAdministratorCommand, Unit>
{
    public async Task<Unit> Handle(EnsureAdministratorCommand request, CancellationToken cancellationToken)
    {
        var email = Email.Create(request.Email);
        var user = await users.GetByEmailAsync(email, cancellationToken);
        var utcNow = clock.UtcNow;

        // No existe: se crea ya activo y con rol Administrator.
        if (user is null)
        {
            var admin = User.CreateAdministrator(
                request.FullName, email, passwordHash: passwordHasher.Hash(request.Password), utcNow);
            await users.AddAsync(admin, cancellationToken);
            await users.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }

        // Existe y está bien: no se toca nada, tampoco su contraseña.
        if (user.Role == Role.Administrator && user.IsActive && !user.IsDisabled)
            return Unit.Value;

        // Existe pero perdió el rol o lo desactivaron: se restaura, sin tocar su contraseña.
        user.RestoreAdministratorAccess(utcNow);
        await users.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
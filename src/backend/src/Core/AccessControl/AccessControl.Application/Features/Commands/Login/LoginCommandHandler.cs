using AccessControl.Application.Abstractions;
using AccessControl.Domain.Sessions;
using AccessControl.Domain.Users.Exceptions;
using Accessontrol.Application.Features.Login;
using MediatR;
using Shared.Application.Abstractions;

namespace AccessControl.Application.Features.Login;

internal sealed class LoginCommandHandler(
    IUserRepository users,
    ISessionRepository sessions,
    IPasswordHasher passwordHasher,
    ITokenIssuer tokenIssuer,
    IClock clock) : IRequestHandler<LoginCommand, LoginResultDTO>
{
    public async Task<LoginResultDTO> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = Domain.Users.Email.Create(request.Email);
        var user = await users.GetByEmailAsync(email, cancellationToken);
        var utcNow = clock.UtcNow;

        // 1. Correo inexistente
        if (user is null)
        {
            passwordHasher.SimulateVerification(request.Password);
            throw new InvalidCredentialsException();
        }

        // 2. Bloqueada
        user.EnsureNotLocked(utcNow);

        // 3. Contraseña incorrecta
        if (!passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            user.RegisterFailedLogin(utcNow);
            await users.SaveChangesAsync(cancellationToken);
            throw new InvalidCredentialsException();
        }

        // 4. Solo quien conoce la contraseña llega aquí
        user.EnsureCanSignIn();

        // 5. Éxito
        user.RegisterSuccessfulLogin();

        var session = Session.Start(user.Id, utcNow, tokenIssuer.Lifetime);
        await sessions.AddAsync(session, cancellationToken);

        // Los repositorios comparten el DbContext del módulo (uno por petición), así que este
        // único guardado persiste el contador del usuario y la sesión en una sola transacción.
        await sessions.SaveChangesAsync(cancellationToken);

        var token = tokenIssuer.Issue(user.Id, session.Id, session.CreatedAt, session.ExpiresAt);
        return new LoginResultDTO(token.Value, token.ExpiresAt);
    }
}
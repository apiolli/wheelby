using AccessControl.Application.Abstractions;
using AccessControl.Application.Common;
using AccessControl.Contracts;
using AccessControl.Domain.Sessions.Exceptions;
using AccessControl.Domain.Users.Exceptions;
using MediatR;
using Shared.Application.Abstractions;

namespace AccessControl.Application.Features.ChangePassword;

internal sealed class ChangePasswordCommandHandler(
    IUserRepository users,
    ISessionRepository sessions,
    IPasswordHasher passwordHasher,
    ITokenIssuer tokenIssuer,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<ChangePasswordCommand, LoginResultDTO>
{
    public async Task<LoginResultDTO> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(currentUser.Id, cancellationToken)
            ?? throw new InvalidSessionException();

        if (!passwordHasher.Verify(user.PasswordHash, request.CurrentPassword))
            throw new InvalidCurrentPasswordException();

        var utcNow = clock.UtcNow;

        user.ChangePassword(passwordHasher.Hash(request.NewPassword));
        await SessionRevocation.RevokeAllAsync(sessions, user.Id, utcNow, cancellationToken);

        // Este guardado persiste todo en una transacción (los repositorios comparten el DbContext).
        var token = await SessionStart.StartAsync(sessions, tokenIssuer, user.Id, utcNow, cancellationToken);

        return new LoginResultDTO(token.Value, token.ExpiresAt);
    }
}

using AccessControl.Application.Abstractions;
using AccessControl.Application.Common;
using AccessControl.Domain.Users;
using AccessControl.Domain.Users.Exceptions;
using MediatR;
using Shared.Application.Abstractions;

namespace AccessControl.Application.Features.ResetPassword;

internal sealed class ResetPasswordCommandHandler(
    IUserRepository users,
    ISessionRepository sessions,
    IOneTimeTokenGenerator tokenGenerator,
    IPasswordHasher passwordHasher,
    IClock clock) : IRequestHandler<ResetPasswordCommand, Unit>
{
    public async Task<Unit> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var email = Email.Create(request.Email);
        var user = await users.GetByEmailAsync(email, cancellationToken)
            ?? throw new InvalidPasswordResetTokenException();

        var utcNow = clock.UtcNow;

        user.ResetPassword(tokenGenerator.Hash(request.Code), passwordHasher.Hash(request.NewPassword), utcNow);
        await SessionRevocation.RevokeAllAsync(sessions, user.Id, utcNow, cancellationToken);
        await users.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

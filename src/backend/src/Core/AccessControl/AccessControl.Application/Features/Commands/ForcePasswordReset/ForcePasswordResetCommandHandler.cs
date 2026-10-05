using AccessControl.Application.Abstractions;
using AccessControl.Application.Common;
using AccessControl.Domain.Users;
using MediatR;
using Notifications.Contracts;
using Shared.Application.Abstractions;
using SharedKernel.Exceptions;

namespace AccessControl.Application.Features.ForcePasswordReset;

internal sealed class ForcePasswordResetCommandHandler(
    IUserRepository users,
    ISessionRepository sessions,
    IOneTimeTokenGenerator tokenGenerator,
    IPasswordHasher passwordHasher,
    IEmailQueue emailQueue,
    IClock clock) : IRequestHandler<ForcePasswordResetCommand, Unit>
{
    public async Task<Unit> Handle(ForcePasswordResetCommand request, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("El usuario no existe.");

        var utcNow = clock.UtcNow;

        var unusableHash = passwordHasher.Hash(tokenGenerator.Generate().Value);
        var code = tokenGenerator.Generate();
        user.ForcePasswordReset(unusableHash, code.Hash, utcNow);

        await SessionRevocation.RevokeAllAsync(sessions, user.Id, utcNow, cancellationToken);
        await users.SaveChangesAsync(cancellationToken);

        await emailQueue.EnqueueAsync(
            user.Email.Value,
            PasswordResetEmail.Subject,
            PasswordResetEmail.BuildBody(user.FullName, code.Value, User.PasswordResetTokenLifetime, forcedByAdministrator: true),
            cancellationToken);

        return Unit.Value;
    }
}

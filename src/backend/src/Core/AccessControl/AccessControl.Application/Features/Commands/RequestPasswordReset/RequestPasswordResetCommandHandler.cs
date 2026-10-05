using AccessControl.Application.Abstractions;
using AccessControl.Application.Common;
using AccessControl.Domain.Users;
using MediatR;
using Notifications.Contracts;
using Shared.Application.Abstractions;

namespace AccessControl.Application.Features.RequestPasswordReset;

internal sealed class RequestPasswordResetCommandHandler(
    IUserRepository users,
    IOneTimeTokenGenerator tokenGenerator,
    IEmailQueue emailQueue,
    IClock clock) : IRequestHandler<RequestPasswordResetCommand, Unit>
{
    public async Task<Unit> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var email = Email.Create(request.Email);
        var user = await users.GetByEmailAsync(email, cancellationToken);

        if (user is null || !user.IsActive || user.IsDisabled)
            return Unit.Value;

        var token = tokenGenerator.Generate();
        user.IssuePasswordResetToken(token.Hash, clock.UtcNow);
        await users.SaveChangesAsync(cancellationToken);

        await emailQueue.EnqueueAsync(
            email.Value,
            PasswordResetEmail.Subject,
            PasswordResetEmail.BuildBody(user.FullName, token.Value, User.PasswordResetTokenLifetime, forcedByAdministrator: false),
            cancellationToken);

        return Unit.Value;
    }
}

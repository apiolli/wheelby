using AccessControl.Application.Abstractions;
using AccessControl.Application.Common;
using AccessControl.Domain.Users;
using MediatR;
using Notifications.Contracts;
using Shared.Application.Abstractions;

namespace AccessControl.Application.Features.ResendActivation;

internal sealed class ResendActivationCommandHandler(
    IUserRepository users,
    IOneTimeTokenGenerator tokenGenerator,
    IActivationLinkBuilder linkBuilder,
    IEmailQueue emailQueue,
    IClock clock) : IRequestHandler<ResendActivationCommand, Unit>
{
    public async Task<Unit> Handle(ResendActivationCommand request, CancellationToken cancellationToken)
    {
        var email = Email.Create(request.Email);
        var user = await users.GetByEmailAsync(email, cancellationToken);

        if (user is null || user.IsActive)
            return Unit.Value;

        var token = tokenGenerator.Generate();
        user.IssueActivationToken(token.Hash, clock.UtcNow); // reemplaza el anterior
        await users.SaveChangesAsync(cancellationToken);

        var link = linkBuilder.Build(user.Id, token.Value);
        await emailQueue.EnqueueAsync(
            email.Value,
            ActivationEmail.Subject,
            ActivationEmail.BuildBody(user.FullName, link, User.ActivationTokenLifetime),
            cancellationToken);

        return Unit.Value;
    }
}
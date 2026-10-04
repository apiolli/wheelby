using AccessControl.Application.Abstractions;
using AccessControl.Application.Common;
using AccessControl.Domain.Users;
using MediatR;
using Notifications.Contracts;
using Shared.Application.Abstractions;
using SharedKernel.Exceptions;

namespace AccessControl.Application.Features.Register;

internal sealed class RegisterUserCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IOneTimeTokenGenerator tokenGenerator,
    IActivationLinkBuilder linkBuilder,
    IEmailQueue emailQueue,
    IClock clock) : IRequestHandler<RegisterUserCommand, Guid>
{
    public async Task<Guid> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var email = Email.Create(request.Email);

        if (await users.ExistsByEmailAsync(email, cancellationToken))
            throw new ConflictException("El correo ya está registrado.");

        var utcNow = clock.UtcNow;
        var user = User.Create(request.FullName, email, passwordHasher.Hash(request.Password), utcNow);

        var token = tokenGenerator.Generate();
        user.IssueActivationToken(token.Hash, utcNow);

        await users.AddAsync(user, cancellationToken);
        await users.SaveChangesAsync(cancellationToken);

        var link = linkBuilder.Build(user.Id, token.Value);
        await emailQueue.EnqueueAsync(
            email.Value,
            ActivationEmail.Subject,
            ActivationEmail.BuildBody(user.FullName, link, User.ActivationTokenLifetime),
            cancellationToken);

        return user.Id;
    }
}
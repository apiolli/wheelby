using AccessControl.Contracts;
using FluentValidation;
using MediatR;
using Notifications.Application.Abstractions;
using Notifications.Domain.Inbox;
using Shared.Application.Abstractions;

namespace Notifications.Application.Features.Preferences;

public sealed record NotificationPreferencesDTO(bool EmailEnabled);

// GET /notifications/preferences (RF-NOT-07). Sin userId: siempre la del usuario de la sesión.
public sealed record GetMyPreferencesQuery : IQuery<NotificationPreferencesDTO>;

internal sealed class GetMyPreferencesQueryHandler(
    INotificationPreferenceRepository preferences,
    ICurrentUser currentUser) : IRequestHandler<GetMyPreferencesQuery, NotificationPreferencesDTO>
{
    public async Task<NotificationPreferencesDTO> Handle(GetMyPreferencesQuery request, CancellationToken cancellationToken)
    {
        var preference = await preferences.GetAsync(currentUser.Id, cancellationToken);
        return new NotificationPreferencesDTO(preference?.EmailEnabled ?? NotificationPreference.DefaultEmailEnabled);
    }
}

// PUT /notifications/preferences con { "emailEnabled": false } (RF-NOT-07).
public sealed record UpdateMyPreferencesCommand(bool? EmailEnabled) : ICommand;

internal sealed class UpdateMyPreferencesCommandValidator : AbstractValidator<UpdateMyPreferencesCommand>
{
    public UpdateMyPreferencesCommandValidator()
    {
        RuleFor(x => x.EmailEnabled)
            .NotNull().WithMessage("emailEnabled es obligatorio (true o false).")
            .OverridePropertyName("emailEnabled");
    }
}

internal sealed class UpdateMyPreferencesCommandHandler(
    INotificationPreferenceRepository preferences,
    INotificationsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<UpdateMyPreferencesCommand, Unit>
{
    public async Task<Unit> Handle(UpdateMyPreferencesCommand request, CancellationToken cancellationToken)
    {
        var emailEnabled = request.EmailEnabled!.Value;
        var preference = await preferences.GetAsync(currentUser.Id, cancellationToken);

        if (preference is null)
            await preferences.AddAsync(
                NotificationPreference.Create(currentUser.Id, emailEnabled, clock.UtcNow), cancellationToken);
        else
            preference.SetEmailEnabled(emailEnabled, clock.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

using Permissions.Domain.PermissionRequests.Exceptions;

namespace Permissions.Domain.PermissionRequests;

// RD-04: único lugar donde viven las transiciones permitidas.
// Agregar una transición es agregar una entrada a AllowedTransitions;
// PermissionRequest.TransitionTo es el único código que cambia Status.
public static class PermissionRequestStateMachine
{
    public sealed record TransitionDefinition(
        PermissionRequestStatus From,
        PermissionRequestStatus To,
        IReadOnlySet<PermissionActor> AllowedActors,
        string Condition,
        Func<PermissionRequest, PermissionActor, Guid, DateTime, string?, bool>? Guard);

    // Las transiciones permitidas.
    public static readonly IReadOnlyList<TransitionDefinition> AllowedTransitions =
    [
        new(
            PermissionRequestStatus.Pending,
            PermissionRequestStatus.Approved,
            new HashSet<PermissionActor> { PermissionActor.Administrator },
            "El administrador aprueba la solicitud y no es el solicitante.",
            static (r, _, resolverId, _, _) => resolverId != r.RequesterId),

        new(
            PermissionRequestStatus.Approved,
            PermissionRequestStatus.Applied,
            new HashSet<PermissionActor> { PermissionActor.System },
            "La aplicación es automática, en la misma operación que la aprobación.",
            null),

        new(
            PermissionRequestStatus.Pending,
            PermissionRequestStatus.Rejected,
            new HashSet<PermissionActor> { PermissionActor.Administrator },
            "Motivo obligatorio y un resolutor distinto del solicitante.",
            static (r, _, resolverId, _, reason) =>
                !string.IsNullOrWhiteSpace(reason) && resolverId != r.RequesterId),

        new(
            PermissionRequestStatus.Rejected,
            PermissionRequestStatus.Applied,
            new HashSet<PermissionActor> { PermissionActor.System },
            "La aplicación es automática, en la misma operación que la aprobación.",
            null),
    ];

    // RF-GP-06: transición explícitamente prohibida con su motivo.
    // Cualquier otra combinación fuera de AllowedTransitions también se rechaza.
    public static readonly (PermissionRequestStatus From, PermissionRequestStatus To, string Reason) ExplicitlyForbidden =
        (PermissionRequestStatus.Rejected, PermissionRequestStatus.Applied,
            "Una solicitud rechazada no se puede aplicar.");

    // Estados terminales; ninguna transición parte de ellos.
    public static readonly IReadOnlySet<PermissionRequestStatus> TerminalStates =
    new HashSet<PermissionRequestStatus>
    {
        PermissionRequestStatus.Applied,
        PermissionRequestStatus.Rejected
    };

    public static void Validate(
        PermissionRequest request,
        PermissionRequestStatus target,
        PermissionActor actor,
        Guid resolverId,
        DateTime utcNow,
        string? rejectionReason)
    {
        ArgumentNullException.ThrowIfNull(request);

        var from = request.Status;

        if (TerminalStates.Contains(from) && target != PermissionRequestStatus.Applied)
            throw new InvalidPermissionRequestTransitionException(
                $"La solicitud en estado {from} es terminal y no admite más transiciones.");

        var definition = AllowedTransitions.FirstOrDefault(t => t.From == from && t.To == target);
        if (definition is null)
            throw new InvalidPermissionRequestTransitionException(
                $"Transición {from} → {target} no permitida.");

        if (!definition.AllowedActors.Contains(actor))
            throw new PermissionRequestTransitionNotAllowedException(
                $"El actor {actor} no puede ejecutar la transición {from} → {target}.");

        if (definition.Guard is not null
            && !definition.Guard(request, actor, resolverId, utcNow, rejectionReason))
        {
            if (actor == PermissionActor.Administrator && resolverId == request.RequesterId)
                throw new CannotResolveOwnPermissionRequestException(
                    "No puedes resolver tu propia solicitud de permiso.");

            if (target == PermissionRequestStatus.Rejected && string.IsNullOrWhiteSpace(rejectionReason))
                throw new RejectionReasonRequiredException("El motivo del rechazo es obligatorio.");

            throw new PermissionRequestTransitionNotAllowedException(
                $"No se cumple la condición de la transición {from} → {target}: {definition.Condition}");
        }
    }
}

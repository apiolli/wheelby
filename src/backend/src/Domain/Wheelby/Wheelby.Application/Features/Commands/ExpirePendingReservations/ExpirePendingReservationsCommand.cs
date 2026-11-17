using Shared.Application.Abstractions;

namespace Wheelby.Application.Features.ExpirePendingReservations;

// Lo ejecuta el Worker. Devuelve cuántas solicitudes expiró en esta pasada.
public sealed record ExpirePendingReservationsCommand(int BatchSize = 100) : ICommand<int>;

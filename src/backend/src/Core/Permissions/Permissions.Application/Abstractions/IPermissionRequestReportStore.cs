namespace Permissions.Application.Abstractions;

// RF-REP-01 y 03: la agregación la hace la base (GROUP BY con COUNT), no la memoria.
public interface IPermissionRequestReportStore
{
    // Solicitudes con CreatedAt en [startUtc, endUtc), agrupadas por año, mes y estado.
    // requesterId null = todas (Administrador); con valor = solo las de ese solicitante.
    Task<IReadOnlyList<PermissionRequestMonthStatusCount>> CountByMonthAndStatusAsync(
        DateTime startUtc, DateTime endUtc, Guid? requesterId, CancellationToken cancellationToken);
}

public sealed record PermissionRequestMonthStatusCount(int Year, int Month, string Status, int Count);

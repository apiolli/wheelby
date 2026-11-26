using AccessControl.Contracts;
using FluentValidation;
using MediatR;
using Permissions.Application.Abstractions;
using Shared.Application.Abstractions;
using Shared.Application.Reports;

namespace Permissions.Application.Features.PermissionRequestsReport;

// RF-REP-01: GET /reports/permission-requests?from=&to=. Solicitudes por mes (UTC) y estado.
public sealed record PermissionRequestsReportQuery(string? From, string? To) : IQuery<PermissionRequestsReportDTO>;

// period = "yyyy-MM" del CreatedAt en UTC. Ninguna fila lleva datos de una solicitud.
public sealed record PermissionRequestsReportRow(string Period, string Status, int Count);

// scope: "all" para el Administrador, "own" para el Estándar (RF-REP-04).
public sealed record PermissionRequestsReportDTO(
    DateOnly From,
    DateOnly To,
    string Scope,
    int Total,
    IReadOnlyList<PermissionRequestsReportRow> Rows);

internal sealed class PermissionRequestsReportQueryValidator : AbstractValidator<PermissionRequestsReportQuery>
{
    public PermissionRequestsReportQueryValidator() => this.AddReportRangeRules(x => x.From, x => x.To);
}

internal sealed class PermissionRequestsReportQueryHandler(
    IPermissionRequestReportStore reports,
    ICurrentUser currentUser) : IRequestHandler<PermissionRequestsReportQuery, PermissionRequestsReportDTO>
{
    public async Task<PermissionRequestsReportDTO> Handle(
        PermissionRequestsReportQuery request, CancellationToken cancellationToken)
    {
        var range = ReportDateRange.Parse(request.From, request.To);

        // RD-06: la regla "solo lo propio" se decide aquí, con la sesión. No hay parámetro
        // para pedir los datos de otro usuario.
        var isAdministrator = currentUser.Role == AccessControlRoles.Administrator;
        Guid? requesterId = isAdministrator ? null : currentUser.Id;

        var groups = await reports.CountByMonthAndStatusAsync(range.StartUtc, range.EndUtc, requesterId, cancellationToken);

        // El periodo y el orden se arman sobre los grupos ya agregados por la base.
        var rows = groups
            .Where(g => g.Count > 0)
            .Select(g => new PermissionRequestsReportRow($"{g.Year:0000}-{g.Month:00}", g.Status, g.Count))
            .OrderBy(r => r.Period, StringComparer.Ordinal)
            .ThenBy(r => r.Status, StringComparer.Ordinal)
            .ToList();

        return new PermissionRequestsReportDTO(
            range.From, range.To, isAdministrator ? "all" : "own", rows.Sum(r => r.Count), rows);
    }
}

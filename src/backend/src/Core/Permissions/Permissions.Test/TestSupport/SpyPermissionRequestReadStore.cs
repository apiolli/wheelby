using Permissions.Application.Abstractions;
using Permissions.Application.Common;
using Permissions.Application.DTOs;

namespace Permissions.Test.Support;

// Espía del read store: registra los argumentos y devuelve un resultado fijo.
public sealed class SpyPermissionRequestReadStore : IPermissionRequestReadStore
{
    public Guid? SolicitadoA { get; private set; }
    public int? Pagina { get; private set; }
    public int? TamanoPagina { get; private set; }

    public PagedResult<PermissionRequestSummaryDTO> Resultado { private get; set; } =
        new([], 1, 20, 0);

    public Task<PagedResult<PermissionRequestSummaryDTO>> ListPendingAsync(
        int page, int pageSize, CancellationToken cancellationToken)
    {
        Pagina = page;
        TamanoPagina = pageSize;
        return Task.FromResult(Resultado);
    }

    public Task<PagedResult<PermissionRequestSummaryDTO>> ListByRequesterAsync(
        Guid requesterId, int page, int pageSize, CancellationToken cancellationToken)
    {
        SolicitadoA = requesterId;
        Pagina = page;
        TamanoPagina = pageSize;
        return Task.FromResult(Resultado);
    }
}

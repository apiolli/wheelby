using MediatR;
using Permissions.Application.Common;
using Permissions.Application.DTOs;
using Permissions.Application.Features.ListMyRequests;
using Permissions.Test.Support;

namespace Permissions.Test.Application;

public sealed class ListMyRequestsHandlerTests
{
    private static readonly Guid SolicitanteId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task RF_GP_02_Listar_mis_solicitudes_usa_el_usuario_actual()
    {
        // Arrange
        var reader = new SpyPermissionRequestReadStore();
        var resultado = new PagedResult<PermissionRequestSummaryDTO>([], 1, 20, 0);
        reader.Resultado = resultado;
        var handler = new ListMyRequestsQueryHandler(reader, new FixedCurrentUser(SolicitanteId));

        // Act
        var respuesta = await handler.Handle(new ListMyRequestsQuery(), default);

        // Assert
        Assert.Equal(SolicitanteId, reader.SolicitadoA);
        Assert.Same(resultado, respuesta);
    }
}

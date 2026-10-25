using Permissions.Domain.PermissionRequests;
using SharedKernel.Exceptions;

namespace Permissions.Test.Domain;

public sealed class PermissionRequestStateMachineTests
{
    private static readonly DateTime Ahora = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Despues = Ahora.AddMinutes(5);
    private static readonly DateTime MasTarde = Ahora.AddMinutes(10);
    private static readonly Guid SolicitanteId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AdministradorId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    // Conjunto legal escrito a mano, copiado de la tabla del requisito:
    // Pending → Approved (Administrator), Approved → Applied (System) y
    // Pending → Rejected (Administrator). No se lee de AllowedTransitions:
    // una prueba que decide con la tabla de producción daría por buena
    // cualquier transición que alguien agregue.
    private static readonly List<(PermissionRequestStatus Origen, PermissionRequestStatus Destino, PermissionActor Actor)> Legales =
    [
        (PermissionRequestStatus.Pending, PermissionRequestStatus.Approved, PermissionActor.Administrator),
        (PermissionRequestStatus.Approved, PermissionRequestStatus.Applied, PermissionActor.System),
        (PermissionRequestStatus.Pending, PermissionRequestStatus.Rejected, PermissionActor.Administrator),
    ];

    // Snapshot independiente del estado esperado tras llegar a cada estado.
    private static (PermissionRequestStatus Estado, Guid? Aprobador, DateTime? Resuelta, string? Motivo)
        DatosEsperadosDe(PermissionRequestStatus estado)
        => estado switch
        {
            PermissionRequestStatus.Pending => (estado, null, null, null),
            PermissionRequestStatus.Approved => (estado, AdministradorId, Despues, null),
            PermissionRequestStatus.Applied => (estado, AdministradorId, Despues, null),
            PermissionRequestStatus.Rejected => (estado, AdministradorId, Despues, "No corresponde"),
            _ => throw new ArgumentOutOfRangeException(nameof(estado)),
        };

    private static PermissionRequest SolicitudEn(PermissionRequestStatus estado)
    {
        var solicitud = PermissionRequest.Create(SolicitanteId, "core.demo-action", Ahora);
        switch (estado)
        {
            case PermissionRequestStatus.Pending:
                return solicitud;
            case PermissionRequestStatus.Approved:
                solicitud.Approve(AdministradorId, Despues);
                return solicitud;
            case PermissionRequestStatus.Applied:
                solicitud.Approve(AdministradorId, Despues);
                solicitud.MarkApplied(Despues);
                return solicitud;
            case PermissionRequestStatus.Rejected:
                solicitud.Reject(AdministradorId, Despues, "No corresponde");
                return solicitud;
            default:
                return solicitud;
        }
    }

    public static IEnumerable<object[]> TodasLasTransiciones()
    {
        foreach (var origen in Enum.GetValues<PermissionRequestStatus>())
        {
            foreach (var destino in Enum.GetValues<PermissionRequestStatus>())
            {
                foreach (var actor in Enum.GetValues<PermissionActor>())
                {
                    var esLegal = Legales.Contains((origen, destino, actor));
                    yield return [origen, destino, actor, esLegal];
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(TodasLasTransiciones))]
    public void RF_GP_06_Todas_las_transiciones_fuera_de_la_tabla_son_rechazadas(
        PermissionRequestStatus origen,
        PermissionRequestStatus destino,
        PermissionActor actor,
        bool esLegal)
    {
        // Arrange
        var solicitud = SolicitudEn(origen);
        var esperado = DatosEsperadosDe(origen);

        // Act
        var exception = Record.Exception(() => solicitud.TransitionTo(
            destino,
            actor,
            actor == PermissionActor.System ? Guid.Empty : AdministradorId,
            MasTarde,
            "No corresponde"));

        // Assert
        if (esLegal)
        {
            Assert.Null(exception);
            Assert.Equal(destino, solicitud.Status);
            return;
        }

        Assert.NotNull(exception);
        Assert.IsAssignableFrom<AppException>(exception);
        Assert.Equal(esperado.Estado, solicitud.Status);
        Assert.Equal(esperado.Aprobador, solicitud.ApproverId);
        Assert.Equal(esperado.Resuelta, solicitud.ResolvedAt);
        Assert.Equal(esperado.Motivo, solicitud.RejectionReason);
    }

    [Fact]
    public void RF_GP_06_Tabla_de_transiciones_declarada_es_exacta()
    {
        // Arrange
        // Conjunto legal escrito a mano, copiado de la tabla del requisito.
        var legales = new HashSet<(PermissionRequestStatus, PermissionRequestStatus)>
        {
            (PermissionRequestStatus.Pending, PermissionRequestStatus.Approved),
            (PermissionRequestStatus.Approved, PermissionRequestStatus.Applied),
            (PermissionRequestStatus.Pending, PermissionRequestStatus.Rejected),
        };

        // Act
        var declarada = PermissionRequestStateMachine.AllowedTransitions
            .Select(t => (t.From, t.To))
            .ToHashSet();
        var prohibida = (
            PermissionRequestStateMachine.ExplicitlyForbidden.From,
            PermissionRequestStateMachine.ExplicitlyForbidden.To);
        var terminales = new HashSet<PermissionRequestStatus>
        {
            PermissionRequestStatus.Applied,
            PermissionRequestStatus.Rejected,
        };

        // Assert
        // Ni más ni menos: exactamente los tres pares del requisito.
        Assert.True(legales.SetEquals(declarada));
        Assert.Equal(3, PermissionRequestStateMachine.AllowedTransitions.Count);
        // La prohibida explícita no es legal y su par es el documentado.
        Assert.DoesNotContain(declarada, par => par == prohibida);
        Assert.Equal(
            (PermissionRequestStatus.Rejected, PermissionRequestStatus.Applied),
            prohibida);
        // Solo quedan dos terminales y ninguna transición parte de ellos.
        Assert.True(terminales.SetEquals(PermissionRequestStateMachine.TerminalStates));
    }
}

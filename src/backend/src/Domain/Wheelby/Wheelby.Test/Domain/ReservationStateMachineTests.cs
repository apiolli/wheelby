using Wheelby.Domain.Reservations;
using Wheelby.Domain.Reservations.Exceptions;

// RF-NEG-03, RF-NEG-04, RF-NEG-05, RD-04: suite de la máquina de estados de la reserva.
// Todo escrito a mano a partir de docs/maquina-de-estados.md, nunca leído de AllowedTransitions.
public class ReservationStateMachineTests
{
    private static readonly DateTime T0 = new(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime T1 = new(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime T2 = new(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime T3 = new(2026, 11, 4, 10, 0, 0, DateTimeKind.Utc);

    private static Reservation CreatePendingReservation(DateTime utcNow)
        => Reservation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            T1,
            T2,
            utcNow);

    // ---------------------------------------------------------------
    // 5 filas permitidas: una prueba por fila, actor y hora válidos.
    // ---------------------------------------------------------------

    [Fact]
    public void RF_NEG_03_Pending_a_Confirmed_con_Host_y_hora_valida_transiciona()
    {
        // Arrange
        var reservation = CreatePendingReservation(T0);

        // Act
        reservation.TransitionTo(ReservationStatus.Confirmed, ReservationActor.Host, T0, null);

        // Assert
        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
    }

    [Fact]
    public void RF_NEG_03_Pending_a_Cancelled_con_Renter_antes_del_inicio_transiciona()
    {
        // Arrange
        var reservation = CreatePendingReservation(T0);

        // Act
        reservation.TransitionTo(ReservationStatus.Cancelled, ReservationActor.Renter, T0, "Ya no lo necesito");

        // Assert
        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
        Assert.Equal("Ya no lo necesito", reservation.CancellationReason);
    }

    [Fact]
    public void RF_NEG_03_Pending_a_Cancelled_con_Host_antes_del_inicio_y_motivo_transiciona()
    {
        // Arrange
        var reservation = CreatePendingReservation(T0);

        // Act
        reservation.TransitionTo(ReservationStatus.Cancelled, ReservationActor.Host, T0, "El vehículo se averió");

        // Assert
        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
        Assert.Equal("El vehículo se averió", reservation.CancellationReason);
    }

    [Fact]
    public void RF_NEG_03_Confirmed_a_InProgress_con_Host_durante_el_rango_transiciona()
    {
        // Arrange
        var reservation = CreatePendingReservation(T0);
        reservation.TransitionTo(ReservationStatus.Confirmed, ReservationActor.Host, T0, null);

        // Act
        reservation.TransitionTo(ReservationStatus.InProgress, ReservationActor.Host, T1, null);

        // Assert
        Assert.Equal(ReservationStatus.InProgress, reservation.Status);
    }

    [Fact]
    public void RF_NEG_03_InProgress_a_Completed_con_Host_despues_del_fin_transiciona()
    {
        // Arrange
        var reservation = CreatePendingReservation(T0);
        reservation.TransitionTo(ReservationStatus.Confirmed, ReservationActor.Host, T0, null);
        reservation.TransitionTo(ReservationStatus.InProgress, ReservationActor.Host, T1, null);

        // Act
        reservation.TransitionTo(ReservationStatus.Completed, ReservationActor.Host, T2, null);

        // Assert
        Assert.Equal(ReservationStatus.Completed, reservation.Status);
    }

    // ---------------------------------------------------------------
    // Cada fila, actor incorrecto: el estado no cambia.
    // ---------------------------------------------------------------

    [Fact]
    public void RF_NEG_03_Pending_a_Confirmed_con_Renter_actor_incorrecto_se_rechaza()
    {
        // Arrange
        var reservation = CreatePendingReservation(T0);

        // Act
        var act = () => reservation.TransitionTo(
            ReservationStatus.Confirmed, ReservationActor.Renter, T0, null);

        // Assert
        var exception = Assert.Throws<ReservationTransitionNotAllowedException>(act);
        Assert.Contains("Renter", exception.Message);
        Assert.Equal(ReservationStatus.Pending, reservation.Status);
    }

    [Fact]
    public void RF_NEG_03_Pending_a_Cancelled_con_actor_desconocido_se_rechaza()
    {
        // Arrange
        var reservation = CreatePendingReservation(T0);

        // Act
        var act = () => reservation.TransitionTo(
            ReservationStatus.Cancelled, (ReservationActor)99, T0, "motivo");

        // Assert
        Assert.Throws<ReservationTransitionNotAllowedException>(act);
        Assert.Equal(ReservationStatus.Pending, reservation.Status);
    }

    [Fact]
    public void RF_NEG_03_Confirmed_a_InProgress_con_Renter_actor_incorrecto_se_rechaza()
    {
        // Arrange
        var reservation = CreatePendingReservation(T0);
        reservation.TransitionTo(ReservationStatus.Confirmed, ReservationActor.Host, T0, null);

        // Act
        var act = () => reservation.TransitionTo(
            ReservationStatus.InProgress, ReservationActor.Renter, T1, null);

        // Assert
        Assert.Throws<ReservationTransitionNotAllowedException>(act);
        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
    }

    [Fact]
    public void RF_NEG_03_Confirmed_a_Cancelled_con_actor_desconocido_se_rechaza()
    {
        // Arrange
        var reservation = CreatePendingReservation(T0);
        reservation.TransitionTo(ReservationStatus.Confirmed, ReservationActor.Host, T0, null);

        // Act
        var act = () => reservation.TransitionTo(
            ReservationStatus.Cancelled, (ReservationActor)99, T0, "motivo");

        // Assert
        Assert.Throws<ReservationTransitionNotAllowedException>(act);
        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
    }

    [Fact]
    public void RF_NEG_03_InProgress_a_Completed_con_Renter_actor_incorrecto_se_rechaza()
    {
        // Arrange
        var reservation = CreatePendingReservation(T0);
        reservation.TransitionTo(ReservationStatus.Confirmed, ReservationActor.Host, T0, null);
        reservation.TransitionTo(ReservationStatus.InProgress, ReservationActor.Host, T1, null);

        // Act
        var act = () => reservation.TransitionTo(
            ReservationStatus.Completed, ReservationActor.Renter, T2, null);

        // Assert
        Assert.Throws<ReservationTransitionNotAllowedException>(act);
        Assert.Equal(ReservationStatus.InProgress, reservation.Status);
    }

    // ---------------------------------------------------------------
    // Cada fila con guarda, guarda no cumplida: el estado no cambia.
    // ---------------------------------------------------------------

    [Fact]
    public void RF_NEG_03_Pending_a_Confirmed_despues_de_StartDate_guarda_no_cumplida_se_rechaza()
    {
        // Arrange
        var reservation = CreatePendingReservation(T0);

        // Act: confirmar cuando utcNow (T2) > StartDate (T1)
        var act = () => reservation.TransitionTo(
            ReservationStatus.Confirmed, ReservationActor.Host, T2, null);

        // Assert
        Assert.Throws<ReservationTransitionNotAllowedException>(act);
        Assert.Equal(ReservationStatus.Pending, reservation.Status);
    }

    [Fact]
    public void RF_NEG_03_Confirmed_a_InProgress_antes_de_StartDate_guarda_no_cumplida_se_rechaza()
    {
        // Arrange
        var reservation = CreatePendingReservation(T0);
        reservation.TransitionTo(ReservationStatus.Confirmed, ReservationActor.Host, T0, null);

        // Act: iniciar cuando utcNow (T0) < StartDate (T1)
        var act = () => reservation.TransitionTo(
            ReservationStatus.InProgress, ReservationActor.Host, T0, null);

        // Assert
        Assert.Throws<ReservationTransitionNotAllowedException>(act);
        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
    }

    [Fact]
    public void RF_NEG_03_Confirmed_a_InProgress_despues_de_EndDate_guarda_no_cumplida_se_rechaza()
    {
        // Arrange
        var reservation = CreatePendingReservation(T0);
        reservation.TransitionTo(ReservationStatus.Confirmed, ReservationActor.Host, T0, null);

        // Act: iniciar cuando utcNow (T3) > EndDate (T2)
        var act = () => reservation.TransitionTo(
            ReservationStatus.InProgress, ReservationActor.Host, T3, null);

        // Assert
        Assert.Throws<ReservationTransitionNotAllowedException>(act);
        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
    }

    [Fact]
    public void RF_NEG_03_InProgress_a_Completed_antes_de_EndDate_guarda_no_cumplida_se_rechaza()
    {
        // Arrange
        var reservation = CreatePendingReservation(T0);
        reservation.TransitionTo(ReservationStatus.Confirmed, ReservationActor.Host, T0, null);
        reservation.TransitionTo(ReservationStatus.InProgress, ReservationActor.Host, T1, null);

        // Act: completar cuando utcNow (T1) < EndDate (T2)
        var act = () => reservation.TransitionTo(
            ReservationStatus.Completed, ReservationActor.Host, T1, null);

        // Assert
        Assert.Throws<ReservationTransitionNotAllowedException>(act);
        Assert.Equal(ReservationStatus.InProgress, reservation.Status);
    }

    [Fact]
    public void RF_NEG_03_Pending_a_Cancelled_en_o_despues_de_StartDate_guarda_no_cumplida_se_rechaza()
    {
        // Arrange
        var reservation = CreatePendingReservation(T0);

        // Act: cancelar cuando utcNow (T1) >= StartDate (T1)
        var act = () => reservation.TransitionTo(
            ReservationStatus.Cancelled, ReservationActor.Renter, T1, "motivo");

        // Assert
        Assert.Throws<ReservationTransitionNotAllowedException>(act);
        Assert.Equal(ReservationStatus.Pending, reservation.Status);
        Assert.Null(reservation.CancellationReason);
    }

    [Fact]
    public void RF_NEG_03_Pending_a_Cancelled_con_Host_sin_motivo_guarda_no_cumplida_se_rechaza()
    {
        // Arrange
        var reservation = CreatePendingReservation(T0);

        // Act: Host cancela sin motivo
        var act = () => reservation.TransitionTo(
            ReservationStatus.Cancelled, ReservationActor.Host, T0, null);

        // Assert
        Assert.Throws<ReservationTransitionNotAllowedException>(act);
        Assert.Equal(ReservationStatus.Pending, reservation.Status);
        Assert.Null(reservation.CancellationReason);
    }

    // ---------------------------------------------------------------
    // Transición explícitamente prohibida: InProgress -> Cancelled, con cada actor.
    // ---------------------------------------------------------------

    [Fact]
    public void RF_NEG_04_InProgress_a_Cancelled_con_Renter_es_prohibida_explicitamente()
    {
        // Arrange
        var reservation = CreatePendingReservation(T0);
        reservation.TransitionTo(ReservationStatus.Confirmed, ReservationActor.Host, T0, null);
        reservation.TransitionTo(ReservationStatus.InProgress, ReservationActor.Host, T1, null);

        // Act
        var act = () => reservation.TransitionTo(
            ReservationStatus.Cancelled, ReservationActor.Renter, T2, "motivo");

        // Assert
        var exception = Assert.Throws<InvalidReservationTransitionException>(act);
        Assert.Contains("InProgress", exception.Message);
        Assert.Contains("Cancelled", exception.Message);
        Assert.Equal(ReservationStatus.InProgress, reservation.Status);
        Assert.Null(reservation.CancellationReason);
    }

    [Fact]
    public void RF_NEG_04_InProgress_a_Cancelled_con_Host_es_prohibida_explicitamente()
    {
        // Arrange
        var reservation = CreatePendingReservation(T0);
        reservation.TransitionTo(ReservationStatus.Confirmed, ReservationActor.Host, T0, null);
        reservation.TransitionTo(ReservationStatus.InProgress, ReservationActor.Host, T1, null);

        // Act
        var act = () => reservation.TransitionTo(
            ReservationStatus.Cancelled, ReservationActor.Host, T2, "motivo");

        // Assert
        Assert.Throws<InvalidReservationTransitionException>(act);
        Assert.Equal(ReservationStatus.InProgress, reservation.Status);
        Assert.Null(reservation.CancellationReason);
    }

    // ---------------------------------------------------------------
    // Terminales: desde Completed y desde Cancelled, hacia cada estado y cada actor.
    // ---------------------------------------------------------------

    public static IEnumerable<object[]> TerminalStatesData()
    {
        foreach (var from in new[] { ReservationStatus.Completed, ReservationStatus.Cancelled })
        foreach (var to in new[] { ReservationStatus.Pending, ReservationStatus.Confirmed, ReservationStatus.InProgress, ReservationStatus.Completed, ReservationStatus.Cancelled })
        foreach (var actor in new[] { ReservationActor.Renter, ReservationActor.Host })
            yield return new object[] { from, to, actor };
    }

    [Theory]
    [MemberData(nameof(TerminalStatesData))]
    public void RF_NEG_05_desde_estado_terminal_cualquier_transicion_se_rechaza(
        ReservationStatus from, ReservationStatus to, ReservationActor actor)
    {
        // Arrange
        var reservation = CreatePendingReservation(T0);

        // Llevar la reserva al estado terminal 'from'
        if (from == ReservationStatus.Completed)
        {
            reservation.TransitionTo(ReservationStatus.Confirmed, ReservationActor.Host, T0, null);
            reservation.TransitionTo(ReservationStatus.InProgress, ReservationActor.Host, T1, null);
            reservation.TransitionTo(ReservationStatus.Completed, ReservationActor.Host, T2, null);
        }
        else // Cancelled
        {
            reservation.TransitionTo(ReservationStatus.Cancelled, ReservationActor.Renter, T0, "motivo");
        }

        var statusAntes = reservation.Status;
        var motivoAntes = reservation.CancellationReason;

        // Act
        var act = () => reservation.TransitionTo(to, actor, T3, "motivo");

        // Assert
        Assert.Throws<InvalidReservationTransitionException>(act);
        Assert.Equal(statusAntes, reservation.Status);
        Assert.Equal(motivoAntes, reservation.CancellationReason);
    }

    // ---------------------------------------------------------------
    // Matriz de pares fuera de la tabla: rechazo y estado igual.
    // ---------------------------------------------------------------

    public static IEnumerable<object[]> FueraDeTablaData()
    {
        var paresFueraDeTabla = new (ReservationStatus From, ReservationStatus To)[]
        {
            (ReservationStatus.Pending, ReservationStatus.Pending),
            (ReservationStatus.Pending, ReservationStatus.InProgress),
            (ReservationStatus.Pending, ReservationStatus.Completed),
            (ReservationStatus.Confirmed, ReservationStatus.Pending),
            (ReservationStatus.Confirmed, ReservationStatus.Confirmed),
            (ReservationStatus.Confirmed, ReservationStatus.Completed),
            (ReservationStatus.InProgress, ReservationStatus.Pending),
            (ReservationStatus.InProgress, ReservationStatus.Confirmed),
            (ReservationStatus.InProgress, ReservationStatus.InProgress),
            (ReservationStatus.InProgress, ReservationStatus.Cancelled),
        };

        foreach (var (from, to) in paresFueraDeTabla)
        foreach (var actor in new[] { ReservationActor.Renter, ReservationActor.Host })
            yield return new object[] { from, to, actor };
    }

    [Theory]
    [MemberData(nameof(FueraDeTablaData))]
    public void RF_NEG_04_par_fuera_de_la_tabla_se_rechaza_y_estado_no_cambia(
        ReservationStatus from, ReservationStatus to, ReservationActor actor)
    {
        // Arrange
        var reservation = LlegarEstado(from);

        var statusAntes = reservation.Status;

        // Act
        var act = () => reservation.TransitionTo(to, actor, T3, "motivo");

        // Assert
        Assert.Throws<InvalidReservationTransitionException>(act);
        Assert.Equal(statusAntes, reservation.Status);
    }

    private static Reservation LlegarEstado(ReservationStatus estado)
    {
        var reservation = CreatePendingReservation(T0);

        if (estado == ReservationStatus.Pending)
            return reservation;

        if (estado == ReservationStatus.Confirmed)
        {
            reservation.TransitionTo(ReservationStatus.Confirmed, ReservationActor.Host, T0, null);
            return reservation;
        }

        if (estado == ReservationStatus.InProgress)
        {
            reservation.TransitionTo(ReservationStatus.Confirmed, ReservationActor.Host, T0, null);
            reservation.TransitionTo(ReservationStatus.InProgress, ReservationActor.Host, T1, null);
            return reservation;
        }

        throw new InvalidOperationException($"Estado {estado} no usado como origen fuera de la tabla.");
    }

    // ---------------------------------------------------------------
    // Prueba estructural de la tabla (escrita a mano, nunca leída de AllowedTransitions).
    // ---------------------------------------------------------------

    [Fact]
    public void RF_NEG_03_AllowedTransitions_contiene_exactamente_las_cinco_filas_de_la_tabla()
    {
        // Conjunto legal escrito a mano, copiado de docs/maquina-de-estados.md.
        var esperado = new (ReservationStatus From, ReservationStatus To, ReservationActor[] Actores)[]
        {
            (ReservationStatus.Pending, ReservationStatus.Confirmed, new[] { ReservationActor.Host }),
            (ReservationStatus.Pending, ReservationStatus.Cancelled, new[] { ReservationActor.Renter, ReservationActor.Host }),
            (ReservationStatus.Confirmed, ReservationStatus.InProgress, new[] { ReservationActor.Host }),
            (ReservationStatus.Confirmed, ReservationStatus.Cancelled, new[] { ReservationActor.Renter, ReservationActor.Host }),
            (ReservationStatus.InProgress, ReservationStatus.Completed, new[] { ReservationActor.Host }),
        };

        Assert.Equal(esperado.Length, ReservationStateMachine.AllowedTransitions.Count);

        foreach (var (from, to, actoresEsperados) in esperado)
        {
            var definicion = ReservationStateMachine.AllowedTransitions
                .FirstOrDefault(t => t.From == from && t.To == to);

            Assert.NotNull(definicion);
            Assert.Equal(actoresEsperados.Length, definicion!.AllowedActors.Count);
            foreach (var actor in actoresEsperados)
                Assert.True(definicion.AllowedActors.Contains(actor),
                    $"Falta el actor {actor} en la transición {from} → {to}.");
        }
    }

    [Fact]
    public void RF_NEG_04_ExplicitlyForbidden_es_InProgress_a_Cancelled()
    {
        Assert.Equal(ReservationStatus.InProgress, ReservationStateMachine.ExplicitlyForbidden.From);
        Assert.Equal(ReservationStatus.Cancelled, ReservationStateMachine.ExplicitlyForbidden.To);
        Assert.Contains("no se cancela", ReservationStateMachine.ExplicitlyForbidden.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RF_NEG_05_TerminalStates_es_exactamente_Completed_y_Cancelled()
    {
        Assert.Equal(2, ReservationStateMachine.TerminalStates.Count);
        Assert.Contains(ReservationStatus.Completed, ReservationStateMachine.TerminalStates);
        Assert.Contains(ReservationStatus.Cancelled, ReservationStateMachine.TerminalStates);
    }

    // ---------------------------------------------------------------
    // Creación (RD-07): Reservation.Create rechaza datos inválidos.
    // ---------------------------------------------------------------

    [Fact]
    public void RD_07_Crear_con_vehicleId_vacio_se_rechaza()
    {
        var act = () => Reservation.Create(Guid.Empty, Guid.NewGuid(), T1, T2, T0);
        Assert.Throws<InvalidReservationDataException>(act);
    }

    [Fact]
    public void RD_07_Crear_con_renterId_vacio_se_rechaza()
    {
        var act = () => Reservation.Create(Guid.NewGuid(), Guid.Empty, T1, T2, T0);
        Assert.Throws<InvalidReservationDataException>(act);
    }

    [Fact]
    public void RD_07_Crear_con_fechas_no_UTC_se_rechaza()
    {
        var act = () => Reservation.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            new DateTime(2026, 11, 2, 10, 0, 0),
            new DateTime(2026, 11, 3, 10, 0, 0), T0);
        Assert.Throws<InvalidReservationDataException>(act);
    }

    [Fact]
    public void RD_07_Crear_con_inicio_igual_o_posterior_a_fin_se_rechaza()
    {
        var act = () => Reservation.Create(Guid.NewGuid(), Guid.NewGuid(), T2, T2, T0);
        Assert.Throws<InvalidReservationDataException>(act);

        var act2 = () => Reservation.Create(Guid.NewGuid(), Guid.NewGuid(), T3, T2, T0);
        Assert.Throws<InvalidReservationDataException>(act2);
    }

    [Fact]
    public void RD_07_Crear_con_inicio_no_futuro_se_rechaza()
    {
        var act = () => Reservation.Create(Guid.NewGuid(), Guid.NewGuid(), T0, T2, T0);
        Assert.Throws<InvalidReservationDataException>(act);
    }

    [Fact]
    public void RD_07_Crear_con_datos_validos_devuelve_Pending()
    {
        var reservation = Reservation.Create(Guid.NewGuid(), Guid.NewGuid(), T1, T2, T0);

        Assert.Equal(ReservationStatus.Pending, reservation.Status);
        Assert.NotEqual(Guid.Empty, reservation.Id);
    }
}

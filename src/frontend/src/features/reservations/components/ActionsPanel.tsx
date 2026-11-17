import { useMutation, useQueryClient } from "@tanstack/react-query";
import { m } from "motion/react";

import {
  cancelReservation,
  completeRental,
  confirmReservation,
  rejectReservation,
  startRental,
} from "@/api/wheelby.api";
import { ERROR_CODES, toApiError } from "@/api/errors";
import ConfirmAction from "@/components/custom/ConfirmAction";
import { Button } from "@/components/ui/button";
import ReasonAction from "@/features/profile/components/ReasonAction";
import { queryKeys } from "@/lib/query-keys";
import { showToast } from "@/stores/session.store";
import type { Reservation } from "@/types/types";
import { ACTION_COPY, actionsFor, type ActionKind, type Side } from "../rules";

const RUN: Record<
  ActionKind,
  (id: string, reason: string) => Promise<unknown>
> = {
  confirm: (id) => confirmReservation(id),
  reject: (id, reason) => rejectReservation(id, reason),
  start: (id) => startRental(id),
  complete: (id) => completeRental(id),
  cancel: (id, reason) => cancelReservation(id, reason),
};

// Panel "Acciones": solo lo que el estado permite a quien mira. Cada acción pide confirmación;
// rechazar y cancelar llevan motivo (obligatorio para el anfitrión, opcional para el
// arrendatario). Una acción fuera de su ventana de fechas se ve deshabilitada con la nota.
export default function ActionsPanel({
  reservation,
  side,
  compact,
  onOverlap,
}: {
  reservation: Reservation;
  side: Side;
  // En línea dentro de una tarjeta: botones chicos y nada si no hay acciones.
  compact?: boolean;
  // Confirmar falló porque otra reserva ya bloquea esas fechas (409 vehicle.overlap).
  onOverlap?: (message: string) => void;
}) {
  const queryClient = useQueryClient();
  const actions = actionsFor(reservation, side);

  const act = useMutation({
    mutationFn: ({ kind, reason }: { kind: ActionKind; reason: string }) =>
      RUN[kind](reservation.id, reason),
    onSuccess: (_, { kind }) => {
      showToast(ACTION_COPY[kind].done);
      queryClient.invalidateQueries({
        queryKey: queryKeys.reservation(reservation.id),
      });
      queryClient.invalidateQueries({ queryKey: ["reservations", "mine"] });
    },
  });

  // Si confirmar choca con otra reserva, el aviso va a un banner y el diálogo se cierra.
  const run = async (kind: ActionKind, reason: string) => {
    try {
      await act.mutateAsync({ kind, reason });
    } catch (err) {
      const e = toApiError(err);
      if (kind === "confirm" && e.code === ERROR_CODES.overlap && onOverlap) {
        onOverlap(e.message);
        return;
      }
      throw err;
    }
  };

  if (actions.length === 0 && compact) return null;
  if (actions.length === 0)
    return (
      <p className="text-[14px] text-muted-foreground">
        {side === "viewer"
          ? "Solo las partes de la reserva pueden cambiar su estado."
          : "No hay acciones disponibles en este estado."}
      </p>
    );

  return (
    <ul
      className={
        compact ? "flex flex-wrap justify-end gap-2" : "flex flex-wrap gap-3"
      }
    >
      {actions.map((a) => {
        const copy = ACTION_COPY[a.kind];
        const destructive = copy.destructive;
        const trigger = (
          <Button
            variant={
              destructive
                ? compact
                  ? "outline"
                  : "destructive-outline"
                : "default"
            }
            size={compact ? "sm" : "lg"}
            className="rounded-full"
            disabled={!a.enabled}
          >
            {copy.label}
          </Button>
        );
        return (
          <m.li
            key={a.kind}
            layout
            className={
              compact ? "grid justify-items-end gap-1" : "grid gap-1.5"
            }
          >
            {!a.enabled ? (
              trigger
            ) : a.reason === "none" ? (
              <ConfirmAction
                trigger={trigger}
                title={copy.title}
                description={copy.description}
                confirmLabel={copy.confirm}
                onConfirm={() => run(a.kind, "")}
              />
            ) : (
              <ReasonAction
                id={`${a.kind}-${reservation.id}`}
                optional={a.reason === "optional"}
                trigger={trigger}
                title={copy.title}
                description={copy.description}
                confirmLabel={copy.confirm}
                note={
                  a.reason === "optional"
                    ? "Si quieres, cuéntale al anfitrión por qué."
                    : undefined
                }
                onConfirm={(reason) => run(a.kind, reason)}
              />
            )}
            {a.hint && (
              <p className="px-1 text-[12px] text-muted-foreground">{a.hint}</p>
            )}
          </m.li>
        );
      })}
    </ul>
  );
}

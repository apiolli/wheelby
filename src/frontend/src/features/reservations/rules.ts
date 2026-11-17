import { formatDayShort } from "@/lib/format";
import { ReservationStatus, type Reservation } from "@/types/types";

// Reglas de la máquina de estados del backend (docs/maquina-de-estados.md), vistas desde la
// interfaz. Una transición prohibida no aparece; una permitida fuera de su ventana de fechas
// aparece deshabilitada con la fecha desde la que estará disponible. La API vuelve a validar.

export type Side = "renter" | "host" | "viewer";

export type ActionKind = "confirm" | "reject" | "start" | "complete" | "cancel";

export interface ReservationAction {
  kind: ActionKind;
  enabled: boolean;
  // Por qué está deshabilitada ("Disponible desde el 20 nov").
  hint?: string;
  // El motivo es obligatorio (anfitrión al rechazar o cancelar), opcional o no aplica.
  reason: "required" | "optional" | "none";
}

export function sideOf(r: Reservation, userId: string): Side {
  if (r.renterId === userId) return "renter";
  if (r.ownerId === userId) return "host";
  return "viewer";
}

const from = (iso: string) => `Disponible desde el ${formatDayShort(iso)}`;

export function actionsFor(
  r: Reservation,
  side: Side,
  now = new Date(),
): ReservationAction[] {
  if (side === "viewer") return [];
  const start = new Date(r.startDate);
  const end = new Date(r.endDate);
  const beforeStart = now < start;
  const cancel: ReservationAction = {
    kind: "cancel",
    enabled: beforeStart,
    hint: beforeStart ? undefined : "Ya pasó la fecha de inicio",
    reason: side === "host" ? "required" : "optional",
  };

  switch (r.status) {
    case ReservationStatus.Pending:
      return side === "renter"
        ? [cancel]
        : [
            {
              kind: "confirm",
              enabled: now <= start,
              hint: now <= start ? undefined : "Ya pasó la fecha de inicio",
              reason: "none",
            },
            { kind: "reject", enabled: true, reason: "required" },
          ];
    case ReservationStatus.Confirmed:
      return side === "renter"
        ? [cancel]
        : [
            {
              kind: "start",
              enabled: now >= start && now <= end,
              hint:
                now < start
                  ? from(r.startDate)
                  : now > end
                    ? "Ya pasó la fecha de fin"
                    : undefined,
              reason: "none",
            },
            cancel,
          ];
    case ReservationStatus.InProgress:
      // Un alquiler en curso no se cancela: solo se completa.
      return side === "host"
        ? [
            {
              kind: "complete",
              enabled: now >= end,
              hint: now >= end ? undefined : from(r.endDate),
              reason: "none",
            },
          ]
        : [];
    default:
      return [];
  }
}

// Textos de cada acción.
export const ACTION_COPY: Record<
  ActionKind,
  {
    label: string;
    title: string;
    description: string;
    confirm: string;
    done: string;
    destructive?: boolean;
  }
> = {
  confirm: {
    label: "Confirmar",
    title: "¿Confirmar esta reserva?",
    description:
      "Las fechas quedarán bloqueadas en tu calendario para este arrendatario.",
    confirm: "Confirmar reserva",
    done: "Confirmaste la reserva",
  },
  reject: {
    label: "Rechazar",
    title: "¿Rechazar esta solicitud?",
    description: "El arrendatario verá tu motivo. No se puede deshacer.",
    confirm: "Rechazar solicitud",
    done: "Rechazaste la solicitud",
    destructive: true,
  },
  start: {
    label: "Iniciar alquiler",
    title: "¿Iniciar el alquiler?",
    description:
      "Marca que entregaste el vehículo. Desde ahora la reserva no se puede cancelar.",
    confirm: "Iniciar alquiler",
    done: "El alquiler está en curso",
  },
  complete: {
    label: "Completar",
    title: "¿Completar el alquiler?",
    description:
      "Marca que el vehículo fue devuelto. La reserva queda cerrada.",
    confirm: "Completar alquiler",
    done: "Completaste el alquiler",
  },
  cancel: {
    label: "Cancelar reserva",
    title: "¿Cancelar esta reserva?",
    description:
      "Las fechas quedarán libres para otras personas. No se puede deshacer.",
    confirm: "Cancelar reserva",
    done: "Cancelaste la reserva",
    destructive: true,
  },
};

import { m } from "motion/react";
import { Check, X } from "lucide-react";
import { cn } from "cn";

import { formatDate } from "@/lib/format";
import {
  ReservationStatus,
  type Reservation,
  type ReservationActor,
  type ReservationStatusChange,
} from "@/types/types";
import type { Side } from "../rules";

const STEPS = [
  { status: ReservationStatus.Pending, label: "Solicitada" },
  { status: ReservationStatus.Confirmed, label: "Confirmada" },
  { status: ReservationStatus.InProgress, label: "En curso" },
  { status: ReservationStatus.Completed, label: "Completada" },
] as const;

interface Step {
  label: string;
  done: boolean;
  cancelled?: boolean;
  at?: string;
  who?: string;
  reason?: string | null;
}

// Quién actuó, desde el punto de vista de quien mira.
function actorLabel(actor: ReservationActor, side: Side) {
  if (actor === "System") return "Wheelby (automático)";
  if (actor === "Renter") return side === "renter" ? "Tú" : "El arrendatario";
  return side === "host" ? "Tú" : "El anfitrión";
}

function buildSteps(
  r: Reservation,
  history: ReservationStatusChange[] | undefined,
  side: Side,
): Step[] {
  const changeTo = (status: ReservationStatus) =>
    history?.find((h) => h.to === status);
  const reached = (status: ReservationStatus) =>
    status === ReservationStatus.Pending ||
    Boolean(changeTo(status)) ||
    (!history &&
      STEPS.findIndex((s) => s.status === r.status) >=
        STEPS.findIndex((s) => s.status === status));

  const step = (status: ReservationStatus, label: string): Step => {
    const change = changeTo(status);
    return {
      label,
      done: reached(status),
      at:
        change?.at ??
        (status === ReservationStatus.Pending ? r.createdAt : undefined),
      who: change ? actorLabel(change.actor, side) : undefined,
    };
  };

  if (r.status !== ReservationStatus.Cancelled)
    return STEPS.map((s) => step(s.status, s.label));

  // Cancelada: la línea llega hasta el último paso alcanzado y termina en una X.
  const cancel = changeTo(ReservationStatus.Cancelled);
  const kept = STEPS.map((s) => step(s.status, s.label)).filter((s) => s.done);
  const rejected =
    cancel?.from === ReservationStatus.Pending && cancel.actor === "Host";
  const expired = cancel?.actor === "System";
  return [
    ...kept,
    {
      label: expired
        ? "Expiró sin confirmar"
        : rejected
          ? "Rechazada"
          : "Cancelada",
      done: true,
      cancelled: true,
      at: cancel?.at,
      who: cancel ? actorLabel(cancel.actor, side) : undefined,
      reason: cancel?.reason ?? r.cancellationReason,
    },
  ];
}

// Línea de tiempo vertical: la línea se dibuja hasta el paso actual al entrar, y el último
// paso alcanzado se marca con un "pop" (la key cambia con el estado, así se repite al avanzar).
export default function Timeline({
  reservation,
  history,
  side,
}: {
  reservation: Reservation;
  history?: ReservationStatusChange[];
  side: Side;
}) {
  const steps = buildSteps(reservation, history, side);
  const current = steps.reduce((last, s, i) => (s.done ? i : last), 0);
  const progress = steps.length > 1 ? current / (steps.length - 1) : 0;

  return (
    <ol
      className="relative grid gap-7"
      aria-label="Línea de tiempo de la reserva"
    >
      {/* Riel y progreso, entre el centro del primer y del último punto */}
      <span
        aria-hidden="true"
        className="absolute top-3 bottom-3 left-[11px] w-0.5 rounded-full bg-border-soft"
      />
      <m.span
        aria-hidden="true"
        key={`line-${reservation.status}`}
        initial={{ scaleY: 0 }}
        animate={{ scaleY: progress }}
        transition={{ duration: 0.7, ease: [0.22, 1, 0.36, 1] }}
        className="absolute top-3 bottom-3 left-[11px] w-0.5 origin-top rounded-full bg-primary"
      />
      {steps.map((s, i) => {
        const latest = i === current;
        return (
          <li
            key={s.label}
            className="relative flex gap-4"
            aria-current={latest ? "step" : undefined}
          >
            <span className="relative z-10 grid size-6 shrink-0 place-items-center">
              {s.done ? (
                <m.span
                  key={latest ? `pop-${reservation.status}` : "done"}
                  initial={latest ? { scale: 0 } : false}
                  animate={{ scale: 1 }}
                  transition={{
                    type: "spring",
                    bounce: 0.55,
                    duration: 0.5,
                    delay: latest ? 0.55 : 0,
                  }}
                  className={cn(
                    "grid size-6 place-items-center rounded-full text-primary-foreground",
                    s.cancelled ? "bg-muted-foreground" : "bg-primary",
                  )}
                >
                  {s.cancelled ? (
                    <X className="size-3.5" strokeWidth={3} />
                  ) : (
                    <Check className="size-3.5" strokeWidth={3} />
                  )}
                </m.span>
              ) : (
                <span className="size-6 rounded-full border-2 border-border bg-background" />
              )}
            </span>
            <div
              className={cn(
                "-mt-0.5 min-w-0 text-[14.5px]",
                !s.done && "text-muted-foreground",
              )}
            >
              <p className="font-semibold">{s.label}</p>
              {s.done && (s.at || s.who) && (
                <p className="text-[13px] text-muted-foreground">
                  {[s.at && formatDate(s.at), s.who]
                    .filter(Boolean)
                    .join(" · ")}
                </p>
              )}
              {s.reason && (
                <p className="mt-2 rounded-xl bg-secondary px-3.5 py-2.5 text-[13.5px]">
                  <span className="font-semibold">Motivo:</span> {s.reason}
                </p>
              )}
            </div>
          </li>
        );
      })}
    </ol>
  );
}

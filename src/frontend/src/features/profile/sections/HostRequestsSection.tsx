import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AnimatePresence } from "motion/react";
import { CircleAlert } from "lucide-react";
import { Link, useSearchParams } from "react-router";

import { errorMessage } from "@/api/errors";
import {
  cancelReservation,
  completeRental,
  confirmReservation,
  listMyReservations,
  rejectReservation,
  startRental,
} from "@/api/wheelby.api";
import ConfirmAction from "@/components/custom/ConfirmAction";
import DisabledHint from "@/components/custom/DisabledHint";
import EmptyState from "@/components/custom/EmptyState";
import { CalendarIllustration } from "@/components/custom/Illustrations";
import Pagination from "@/components/custom/Pagination";
import SectionHeading from "@/components/custom/SectionHeading";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button, buttonVariants } from "@/components/ui/button";
import { formatDate } from "@/lib/format";
import { queryKeys } from "@/lib/query-keys";
import { pageParam } from "@/lib/schemas";
import { showToast } from "@/stores/session.store";
import { ReservationStatus, type Reservation } from "@/types/types";
import ReasonAction from "../components/ReasonAction";
import ReservationCard from "../components/ReservationCard";

const PAGE_SIZE = 20;

const GROUPS: { title: string; statuses: ReservationStatus[] }[] = [
  { title: "Por confirmar", statuses: [ReservationStatus.Pending] },
  {
    title: "Confirmadas y en curso",
    statuses: [ReservationStatus.Confirmed, ReservationStatus.InProgress],
  },
  {
    title: "Anteriores",
    statuses: [ReservationStatus.Completed, ReservationStatus.Cancelled],
  },
];

// Reservas de mis vehículos (GET /reservations/mine?as=host): las confirmo, rechazo, inicio,
// completo o cancelo según su estado.
export default function HostRequestsSection() {
  const [params] = useSearchParams();
  const page = pageParam(params);
  const queryClient = useQueryClient();

  const reservations = useQuery({
    queryKey: queryKeys.myReservations("host", page),
    queryFn: () => listMyReservations("host", page, PAGE_SIZE),
  });

  const act = useMutation({
    mutationFn: ({ run }: { run: () => Promise<unknown>; done: string }) =>
      run(),
    onSuccess: (_, { done }) => {
      showToast(done);
      return queryClient.invalidateQueries({
        queryKey: queryKeys.myReservations("host"),
      });
    },
  });
  const run = (fn: () => Promise<unknown>, done: string) =>
    act.mutateAsync({ run: fn, done });

  const items = reservations.data?.items ?? [];

  const actions = (r: Reservation) => {
    const now = new Date();
    const started = now >= new Date(r.startDate);
    const ended = now >= new Date(r.endDate);

    switch (r.status) {
      case ReservationStatus.Pending:
        return (
          <>
            <ReasonAction
              id={`reject-${r.id}`}
              trigger={
                <Button variant="destructive-outline" size="sm">
                  Rechazar
                </Button>
              }
              title="¿Rechazar esta reserva?"
              description="Le mostraremos el motivo a quien la pidió."
              confirmLabel="Rechazar reserva"
              onConfirm={(reason) =>
                run(
                  () => rejectReservation(r.id, reason),
                  "Rechazaste la reserva",
                )
              }
            />
            <ConfirmAction
              trigger={<Button size="sm">Confirmar</Button>}
              title="¿Confirmar esta reserva?"
              description={`Las fechas quedarán bloqueadas para tu vehículo a partir del ${formatDate(r.startDate)}.`}
              confirmLabel="Confirmar reserva"
              onConfirm={() =>
                run(() => confirmReservation(r.id), "Confirmaste la reserva")
              }
            />
          </>
        );
      case ReservationStatus.Confirmed: {
        const start = (
          <Button size="sm" disabled={!started}>
            Iniciar alquiler
          </Button>
        );
        return (
          <>
            {!started && (
              <ReasonAction
                id={`host-cancel-${r.id}`}
                trigger={
                  <Button variant="destructive-outline" size="sm">
                    Cancelar
                  </Button>
                }
                title="¿Cancelar esta reserva confirmada?"
                description="Las fechas quedarán libres. Le mostraremos el motivo a quien la pidió."
                confirmLabel="Cancelar reserva"
                onConfirm={(reason) =>
                  run(
                    () => cancelReservation(r.id, reason),
                    "Cancelaste la reserva",
                  )
                }
              />
            )}
            {started ? (
              <ConfirmAction
                trigger={start}
                title="¿Iniciar el alquiler?"
                description="Confirma que entregaste el vehículo."
                confirmLabel="Iniciar alquiler"
                onConfirm={() =>
                  run(() => startRental(r.id), "El alquiler está en curso")
                }
              />
            ) : (
              <DisabledHint
                hint={`Podrás iniciarlo desde el ${formatDate(r.startDate)}.`}
              >
                {start}
              </DisabledHint>
            )}
          </>
        );
      }
      case ReservationStatus.InProgress: {
        const complete = (
          <Button size="sm" disabled={!ended}>
            Completar alquiler
          </Button>
        );
        return ended ? (
          <ConfirmAction
            trigger={complete}
            title="¿Completar el alquiler?"
            description="Confirma que recibiste el vehículo de vuelta."
            confirmLabel="Completar alquiler"
            onConfirm={() =>
              run(() => completeRental(r.id), "Alquiler completado")
            }
          />
        ) : (
          <DisabledHint
            hint={`Podrás completarlo desde el ${formatDate(r.endDate)}.`}
          >
            {complete}
          </DisabledHint>
        );
      }
      default:
        return undefined;
    }
  };

  return (
    <>
      <SectionHeading
        title="Solicitudes recibidas"
        description="Las reservas de tus vehículos. Confírmalas o recházalas a tiempo."
      />
      {reservations.isPending ? (
        <ul className="grid gap-3" aria-busy="true">
          {[0, 1, 2].map((i) => (
            <li
              key={i}
              className="h-32 animate-pulse rounded-2xl bg-secondary"
            />
          ))}
        </ul>
      ) : reservations.isError ? (
        <Alert variant="destructive">
          <CircleAlert />
          <AlertDescription>
            {errorMessage(reservations.error)}
          </AlertDescription>
        </Alert>
      ) : items.length === 0 ? (
        <EmptyState
          illustration={<CalendarIllustration className="w-37.5" />}
          title="Aún no tienes solicitudes"
          text="Cuando alguien reserve uno de tus vehículos, aparecerá aquí para que lo confirmes."
          action={
            <Link
              to="/perfil/anfitrion"
              className={buttonVariants({ variant: "outline", size: "lg" })}
            >
              Publicar un vehículo
            </Link>
          }
        />
      ) : (
        <div className="grid gap-8">
          {GROUPS.map(({ title, statuses }) => {
            const list = items.filter((r) => statuses.includes(r.status));
            if (list.length === 0) return null;
            return (
              <section key={title}>
                <h2 className="mb-3 text-[13px] font-bold tracking-wide text-muted-foreground uppercase">
                  {title} <span className="tabular-nums">({list.length})</span>
                </h2>
                <ul className="grid gap-3">
                  <AnimatePresence initial={false}>
                    {list.map((r) => (
                      <ReservationCard
                        key={r.id}
                        reservation={r}
                        actions={actions(r)}
                      />
                    ))}
                  </AnimatePresence>
                </ul>
              </section>
            );
          })}
          <Pagination
            page={reservations.data.page}
            totalPages={reservations.data.totalPages}
          />
        </div>
      )}
    </>
  );
}

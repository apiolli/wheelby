import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AnimatePresence } from "motion/react";
import { CircleAlert } from "lucide-react";
import { Link, useSearchParams } from "react-router";

import { errorMessage } from "@/api/errors";
import { cancelReservation, listMyReservations } from "@/api/wheelby.api";
import EmptyState from "@/components/custom/EmptyState";
import { CalendarIllustration } from "@/components/custom/Illustrations";
import Pagination from "@/components/custom/Pagination";
import SectionHeading from "@/components/custom/SectionHeading";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button, buttonVariants } from "@/components/ui/button";
import { queryKeys } from "@/lib/query-keys";
import { pageParam } from "@/lib/schemas";
import { showToast } from "@/stores/session.store";
import { ReservationStatus, type Reservation } from "@/types/types";
import ReasonAction from "../components/ReasonAction";
import ReservationCard from "../components/ReservationCard";

const PAGE_SIZE = 20;
const UPCOMING: ReservationStatus[] = [
  ReservationStatus.Pending,
  ReservationStatus.Confirmed,
  ReservationStatus.InProgress,
];

// Reservas que hice como arrendatario (GET /reservations/mine?as=renter).
export default function ReservationsSection() {
  const [params] = useSearchParams();
  const page = pageParam(params);
  const queryClient = useQueryClient();

  const reservations = useQuery({
    queryKey: queryKeys.myReservations("renter", page),
    queryFn: () => listMyReservations("renter", page, PAGE_SIZE),
  });

  const cancel = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      cancelReservation(id, reason),
    onSuccess: () => {
      showToast("Cancelaste la reserva");
      return queryClient.invalidateQueries({
        queryKey: queryKeys.myReservations("renter"),
      });
    },
  });

  const items = reservations.data?.items ?? [];
  const upcoming = items.filter((r) => UPCOMING.includes(r.status));
  const past = items.filter((r) => !UPCOMING.includes(r.status));

  const actions = (r: Reservation) => {
    // Solo se cancela antes del inicio y mientras no haya terminado (la API lo vuelve a validar).
    const cancellable =
      (r.status === ReservationStatus.Pending ||
        r.status === ReservationStatus.Confirmed) &&
      new Date(r.startDate) > new Date();
    if (!cancellable) return undefined;
    return (
      <ReasonAction
        id={`cancel-${r.id}`}
        optional
        trigger={
          <Button variant="destructive-outline" size="sm">
            Cancelar reserva
          </Button>
        }
        title="¿Cancelar esta reserva?"
        description="Las fechas quedarán libres para otras personas. No se puede deshacer."
        confirmLabel="Cancelar reserva"
        note="Si quieres, cuéntale al anfitrión por qué."
        onConfirm={(reason) => cancel.mutateAsync({ id: r.id, reason })}
      />
    );
  };

  return (
    <>
      <SectionHeading
        title="Mis reservas"
        description="Tus viajes con sus fechas y su estado."
      />
      {reservations.isPending ? (
        <ul className="grid gap-3" aria-busy="true">
          {[0, 1, 2].map((i) => (
            <li
              key={i}
              className="h-28 animate-pulse rounded-2xl bg-secondary"
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
          title="Aún no tienes reservas"
          text="Cuando reserves un vehículo, aparecerá aquí con sus fechas y su estado."
          action={
            <Link to="/" className={buttonVariants({ size: "lg" })}>
              Explorar vehículos
            </Link>
          }
        />
      ) : (
        <div className="grid gap-8">
          {upcoming.length > 0 && (
            <Group title="Próximas" reservations={upcoming} actions={actions} />
          )}
          {past.length > 0 && <Group title="Anteriores" reservations={past} />}
          <Pagination
            page={reservations.data.page}
            totalPages={reservations.data.totalPages}
          />
        </div>
      )}
    </>
  );
}

function Group({
  title,
  reservations,
  actions,
}: {
  title: string;
  reservations: Reservation[];
  actions?: (r: Reservation) => React.ReactNode;
}) {
  return (
    <section>
      <h2 className="mb-3 text-[13px] font-bold tracking-wide text-muted-foreground uppercase">
        {title}
      </h2>
      <ul className="grid gap-3">
        <AnimatePresence initial={false}>
          {reservations.map((r) => (
            <ReservationCard
              key={r.id}
              reservation={r}
              actions={actions?.(r)}
            />
          ))}
        </AnimatePresence>
      </ul>
    </section>
  );
}

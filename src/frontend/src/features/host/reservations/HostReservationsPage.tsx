import { useState } from "react";
import { AnimatePresence, m } from "motion/react";
import { CalendarX2, CircleAlert, UserRound, X } from "lucide-react";
import { Link, useSearchParams } from "react-router";
import { cn } from "cn";

import { errorMessage } from "@/api/errors";
import EmptyState from "@/components/custom/EmptyState";
import { CalendarIllustration } from "@/components/custom/Illustrations";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button, buttonVariants } from "@/components/ui/button";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import ReservationCard from "@/features/profile/components/ReservationCard";
import ActionsPanel from "@/features/reservations/components/ActionsPanel";
import { features } from "@/lib/features";
import { shortId } from "@/lib/format";
import { ENTER } from "@/lib/motion";
import { ReservationStatus, type Reservation } from "@/types/types";
import { useHostReservations } from "../useHostReservations";

type TabId = "solicitudes" | "confirmadas" | "en-curso" | "historial";

const TABS: {
  id: TabId;
  label: string;
  statuses: ReservationStatus[];
  empty: string;
  // Orden: lo próximo primero; el historial, lo más reciente primero.
  order: 1 | -1;
}[] = [
  {
    id: "solicitudes",
    label: "Solicitudes",
    statuses: [ReservationStatus.Pending],
    empty: "No tienes solicitudes nuevas.",
    order: 1,
  },
  {
    id: "confirmadas",
    label: "Confirmadas",
    statuses: [ReservationStatus.Confirmed],
    empty: "No tienes reservas confirmadas.",
    order: 1,
  },
  {
    id: "en-curso",
    label: "En curso",
    statuses: [ReservationStatus.InProgress],
    empty: "No hay alquileres en curso.",
    order: 1,
  },
  {
    id: "historial",
    label: "Historial",
    statuses: [ReservationStatus.Completed, ReservationStatus.Cancelled],
    empty: "Aún no hay reservas terminadas.",
    order: -1,
  },
];

// Reservas recibidas: las de mis vehículos, por pestaña. Las acciones van en línea y siempre
// piden confirmación. Pestaña y vehículo viven en la URL (?pestana=&vehiculo=).
export default function HostReservationsPage() {
  const [params, setParams] = useSearchParams();
  const tabId = (TABS.find((t) => t.id === params.get("pestana"))?.id ??
    "solicitudes") as TabId;
  const vehicleId = params.get("vehiculo");
  const reservations = useHostReservations();
  const [overlap, setOverlap] = useState<string | null>(null);

  const all = (reservations.data ?? []).filter(
    (r) => !vehicleId || r.vehicleId === vehicleId,
  );
  const inTab = (id: TabId) => {
    const tab = TABS.find((t) => t.id === id)!;
    return all
      .filter((r) => tab.statuses.includes(r.status))
      .sort((a, b) => tab.order * a.startDate.localeCompare(b.startDate));
  };
  const tab = TABS.find((t) => t.id === tabId)!;
  const items = inTab(tabId);
  const vehicle = vehicleId
    ? reservations.data?.find((r) => r.vehicleId === vehicleId)
    : undefined;

  const setParam = (key: string, value: string | null) => {
    const next = new URLSearchParams(params);
    if (value === null) next.delete(key);
    else next.set(key, value);
    setParams(next, { replace: true });
    setOverlap(null);
  };

  return (
    <main className="px-gutter pt-6 pb-16">
      <div className="mx-auto grid max-w-240 grid-cols-1 gap-6">
        <m.header {...ENTER} className="grid gap-1">
          <h1 className="text-[26px] leading-tight font-bold tracking-tight md:text-[30px]">
            Reservas recibidas
          </h1>
          <p className="text-[14.5px] text-muted-foreground">
            Responde las solicitudes antes de su fecha de inicio.
          </p>
        </m.header>

        <Tabs
          value={tabId}
          onValueChange={(v) =>
            setParam("pestana", v === "solicitudes" ? null : String(v))
          }
        >
          <TabsList aria-label="Estado de las reservas" className="max-w-full">
            {TABS.map((t) => {
              const count = reservations.data ? inTab(t.id).length : null;
              const featured = t.id === "solicitudes" && (count ?? 0) > 0;
              return (
                <TabsTrigger key={t.id} value={t.id}>
                  {t.label}
                  {count !== null && (
                    <span
                      className={cn(
                        "min-w-5 rounded-full px-1.5 text-center text-[11.5px] leading-5 font-bold tabular-nums",
                        featured
                          ? "bg-highlight text-foreground"
                          : "bg-border-soft text-muted-foreground",
                      )}
                    >
                      {count}
                    </span>
                  )}
                </TabsTrigger>
              );
            })}
          </TabsList>
        </Tabs>

        {vehicleId && (
          <p className="flex flex-wrap items-center gap-2 text-[14px] text-muted-foreground">
            Mostrando solo
            <span className="inline-flex h-8 items-center gap-1 rounded-full bg-primary-soft pr-1 pl-3 font-semibold text-primary">
              {vehicle
                ? `${vehicle.brand} ${vehicle.model}`
                : `Vehículo #${shortId(vehicleId)}`}
              <button
                type="button"
                aria-label="Ver reservas de todos los vehículos"
                onClick={() => setParam("vehiculo", null)}
                className="grid size-6 place-items-center rounded-full outline-none hover:bg-primary/10 focus-visible:ring-2 focus-visible:ring-ring"
              >
                <X className="size-3.5" strokeWidth={2.25} />
              </button>
            </span>
          </p>
        )}

        <AnimatePresence initial={false}>
          {overlap && (
            <m.div
              key="overlap"
              initial={{ opacity: 0, y: -6 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0 }}
            >
              <Alert variant="destructive">
                <CalendarX2 />
                <AlertDescription className="flex items-start justify-between gap-3">
                  {overlap}
                  <button
                    type="button"
                    aria-label="Cerrar aviso"
                    onClick={() => setOverlap(null)}
                    className="grid size-6 shrink-0 place-items-center rounded-full outline-none hover:bg-destructive/10 focus-visible:ring-2 focus-visible:ring-ring"
                  >
                    <X className="size-3.5" />
                  </button>
                </AlertDescription>
              </Alert>
            </m.div>
          )}
        </AnimatePresence>

        {reservations.isPending ? (
          <ul
            className="grid grid-cols-1 gap-3"
            aria-busy="true"
            aria-label="Cargando reservas"
          >
            {[0, 1, 2].map((i) => (
              <li
                key={i}
                className="h-36 animate-pulse rounded-[18px] bg-secondary"
              />
            ))}
          </ul>
        ) : reservations.isError ? (
          <Alert variant="destructive">
            <CircleAlert />
            <AlertDescription>
              {errorMessage(reservations.error)}
              <Button
                variant="outline"
                size="sm"
                className="mt-3 rounded-full"
                onClick={() => reservations.refetch()}
              >
                Reintentar
              </Button>
            </AlertDescription>
          </Alert>
        ) : items.length === 0 ? (
          <EmptyState
            illustration={<CalendarIllustration className="w-37.5" />}
            title={tab.empty}
            text={
              tab.id === "solicitudes"
                ? "Cuando alguien quiera reservar uno de tus vehículos, aparecerá aquí para que lo confirmes."
                : "Las reservas aparecen aquí según avanzan."
            }
            action={
              tab.id === "solicitudes" && features.myVehicles ? (
                <Link
                  to="/anfitrion/vehiculos"
                  className={buttonVariants({
                    variant: "outline",
                    size: "lg",
                    className: "rounded-full",
                  })}
                >
                  Ver mis vehículos
                </Link>
              ) : undefined
            }
          />
        ) : (
          <ul className="grid grid-cols-1 gap-3" aria-label={tab.label}>
            {/* Al confirmar o rechazar, la tarjeta sale de la pestaña y las demás suben. */}
            <AnimatePresence initial={false}>
              {items.map((r) => (
                <ReservationCard
                  key={r.id}
                  reservation={r}
                  meta={<Renter reservation={r} />}
                  actions={
                    tab.id === "historial" ? (
                      features.reservationDetail && (
                        <Link
                          to={`/reservas/${r.id}`}
                          className={buttonVariants({
                            variant: "outline",
                            size: "sm",
                            className: "rounded-full",
                          })}
                        >
                          Ver detalle
                        </Link>
                      )
                    ) : (
                      <ActionsPanel
                        reservation={r}
                        side="host"
                        compact
                        onOverlap={setOverlap}
                      />
                    )
                  }
                />
              ))}
            </AnimatePresence>
          </ul>
        )}
      </div>
    </main>
  );
}

// La API no expone el nombre del arrendatario: se muestra su referencia corta.
function Renter({ reservation: r }: { reservation: Reservation }) {
  return (
    <p className="mt-1 flex items-center gap-1.5 text-muted-foreground">
      <UserRound className="size-3.5 shrink-0" strokeWidth={2} />
      Arrendatario #{shortId(r.renterId)}
    </p>
  );
}

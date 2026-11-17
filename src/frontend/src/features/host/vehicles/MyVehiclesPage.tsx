import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AnimatePresence, m } from "motion/react";
import { CalendarClock, Ellipsis, Plus } from "lucide-react";
import { Link, useLocation, useNavigate, useSearchParams } from "react-router";

import { errorMessage, toApiError } from "@/api/errors";
import { listMyVehicles, setVehicleActive } from "@/api/wheelby.api";
import EmptyState from "@/components/custom/EmptyState";
import { SectionError } from "@/components/custom/ErrorStates";
import {
  ReservationListSkeleton,
  Skeleton,
} from "@/components/custom/Skeletons";
import { CarIllustration } from "@/components/custom/Illustrations";
import Pagination from "@/components/custom/Pagination";
import VehicleImage from "@/features/marketplace/vehicle/VehicleImage";
import { Badge } from "@/components/ui/badge";
import { buttonVariants } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Switch } from "@/components/ui/switch";
import SuccessBanner from "@/features/reservations/components/SuccessBanner";
import { features } from "@/lib/features";
import { formatPrice } from "@/lib/format";
import { ENTER, STAGGER_ITEM, STAGGER_LIST } from "@/lib/motion";
import { queryKeys } from "@/lib/query-keys";
import { pageParam } from "@/lib/schemas";
import { showToast } from "@/stores/session.store";
import {
  ReservationStatus,
  type Reservation,
  type Vehicle,
} from "@/types/types";
import HostSubnav from "../components/HostSubnav";
import { useHostReservations } from "../useHostReservations";
import type { VehicleSavedState } from "./VehicleFormPage";

const PAGE_SIZE = 20;

// Estados que cuentan como "próximas": aún no terminaron.
const UPCOMING: ReservationStatus[] = [
  ReservationStatus.Pending,
  ReservationStatus.Confirmed,
  ReservationStatus.InProgress,
];
// Estados que impiden desactivar el vehículo (el backend responde 409).
const BLOCKING: ReservationStatus[] = [
  ReservationStatus.Confirmed,
  ReservationStatus.InProgress,
];

function summary(reservations: Reservation[] | undefined) {
  const byVehicle = new Map<string, { upcoming: number; blocking: number }>();
  for (const r of reservations ?? []) {
    const entry = byVehicle.get(r.vehicleId) ?? { upcoming: 0, blocking: 0 };
    if (UPCOMING.includes(r.status)) entry.upcoming++;
    if (BLOCKING.includes(r.status)) entry.blocking++;
    byVehicle.set(r.vehicleId, entry);
  }
  return byVehicle;
}

// Mis vehículos (P1): los que publiqué, con sus reservas próximas y, con P4, editar y pausar.
export default function MyVehiclesPage() {
  const [params] = useSearchParams();
  const page = pageParam(params);
  const location = useLocation();
  const saved = (location.state as VehicleSavedState | null)?.saved;

  const vehicles = useQuery({
    queryKey: queryKeys.myVehicles(page),
    queryFn: () => listMyVehicles(page, PAGE_SIZE),
  });
  const reservations = useHostReservations();
  const counts = summary(reservations.data);

  const publishLink = (
    <Link
      to="/anfitrion/vehiculos/nuevo"
      className={buttonVariants({ size: "lg", className: "rounded-full" })}
    >
      <Plus /> Publicar vehículo
    </Link>
  );

  return (
    <main className="px-gutter pt-6 pb-16">
      <div className="mx-auto grid max-w-240 grid-cols-1 gap-6">
        <HostSubnav />
        {saved && (
          <SuccessBanner>
            {saved.created
              ? `Publicamos tu ${saved.name}. Ya está en el catálogo.`
              : `Guardamos los cambios de tu ${saved.name}.`}
          </SuccessBanner>
        )}
        <m.header
          {...ENTER}
          className="flex flex-wrap items-end justify-between gap-4"
        >
          <div>
            <h1 className="text-[26px] leading-tight font-bold tracking-tight md:text-[30px]">
              Mis vehículos
            </h1>
            {vehicles.data && vehicles.data.totalItems > 0 && (
              <p className="mt-1 text-[14.5px] text-muted-foreground">
                {vehicles.data.totalItems}{" "}
                {vehicles.data.totalItems === 1
                  ? "vehículo publicado"
                  : "vehículos publicados"}
              </p>
            )}
          </div>
          {publishLink}
        </m.header>

        {vehicles.isPending ? (
          <ReservationListSkeleton />
        ) : vehicles.isError ? (
          <SectionError
            error={vehicles.error}
            onRetry={() => vehicles.refetch()}
          />
        ) : vehicles.data.items.length === 0 ? (
          <EmptyState
            illustration={<CarIllustration className="w-37.5" />}
            title="Aún no has publicado ningún vehículo."
            text="Publícalo en un par de minutos: marca, modelo, año, placa y precio por día."
            action={publishLink}
          />
        ) : (
          <>
            <m.ul {...STAGGER_LIST} className="grid grid-cols-1 gap-3">
              {vehicles.data.items.map((v) => (
                <VehicleRow
                  key={v.id}
                  vehicle={v}
                  upcoming={counts.get(v.id)?.upcoming}
                  blocking={(counts.get(v.id)?.blocking ?? 0) > 0}
                  countsReady={reservations.isSuccess}
                />
              ))}
            </m.ul>
            <Pagination
              page={vehicles.data.page}
              totalPages={vehicles.data.totalPages}
            />
          </>
        )}
      </div>
    </main>
  );
}

function VehicleRow({
  vehicle: v,
  upcoming = 0,
  blocking,
  countsReady,
}: {
  vehicle: Vehicle;
  upcoming?: number;
  blocking: boolean;
  countsReady: boolean;
}) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const toggle = useMutation({
    mutationFn: (active: boolean) => setVehicleActive(v.id, active),
    onSuccess: (_, active) => {
      showToast(
        active
          ? "Tu vehículo vuelve a aparecer en el catálogo"
          : "Pausamos tu vehículo: no aparece en el catálogo",
      );
      queryClient.invalidateQueries({ queryKey: ["vehicles"] });
    },
    onError: (err) =>
      showToast(
        toApiError(err).code === "vehicle.has-active-reservations"
          ? "Tiene reservas confirmadas o en curso: no se puede desactivar."
          : errorMessage(err),
      ),
  });
  // Mientras la petición viaja, el interruptor ya muestra el valor nuevo.
  const active = toggle.isPending ? toggle.variables : v.isActive;
  // Solo se bloquea apagarlo; volver a activarlo siempre se puede.
  const cannotDeactivate = active && blocking;
  const name = `${v.brand} ${v.model}`;

  return (
    <m.li
      {...STAGGER_ITEM}
      className="rounded-[18px] border border-border p-3 sm:p-4"
    >
      <div className="flex gap-4">
        <div className="aspect-4/3 w-24 shrink-0 self-start overflow-hidden rounded-xl sm:w-36">
          <VehicleImage
            vehicleId={v.id}
            brand={v.brand}
            photoId={v.coverPhotoId}
            muted={!active}
          />
        </div>
        <div className="flex min-w-0 flex-1 flex-col justify-center gap-1.5">
          <div className="flex items-start justify-between gap-3">
            <div className="min-w-0">
              <p className="truncate text-[16px] font-bold">
                {name}{" "}
                <span className="font-normal text-muted-foreground">
                  · {v.year}
                </span>
              </p>
              <p className="text-[14px]">
                <strong>{formatPrice(v.pricePerDay)}</strong>{" "}
                <span className="text-muted-foreground">por día</span>
              </p>
            </div>
            <DropdownMenu>
              <DropdownMenuTrigger
                aria-label={`Opciones de ${name}`}
                className="grid size-9 shrink-0 place-items-center rounded-full outline-none hover:bg-secondary focus-visible:ring-2 focus-visible:ring-ring data-popup-open:bg-secondary"
              >
                <Ellipsis className="size-4.5" />
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem
                  onClick={() =>
                    navigate(`/anfitrion/reservas?vehiculo=${v.id}`)
                  }
                >
                  Ver reservas
                </DropdownMenuItem>
                {features.vehicleEdit && (
                  <DropdownMenuItem
                    onClick={() => navigate(`/anfitrion/vehiculos/${v.id}`)}
                  >
                    Editar
                  </DropdownMenuItem>
                )}
                <DropdownMenuItem
                  onClick={() => navigate(`/vehiculos/${v.id}`)}
                >
                  Ver en el catálogo
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>

          <div className="flex flex-wrap items-center gap-x-4 gap-y-2 text-[13.5px] text-muted-foreground">
            <span className="inline-flex items-center gap-1.5">
              <CalendarClock className="size-4" strokeWidth={1.75} />
              {!countsReady ? (
                <Skeleton className="inline-block h-3 w-24 rounded-full" />
              ) : upcoming === 0 ? (
                "Sin reservas próximas"
              ) : (
                <Link
                  to={`/anfitrion/reservas?vehiculo=${v.id}`}
                  className="font-semibold text-foreground underline-offset-4 hover:underline"
                >
                  {upcoming}{" "}
                  {upcoming === 1 ? "reserva próxima" : "reservas próximas"}
                </Link>
              )}
            </span>

            {features.vehicleEdit && (
              <span className="inline-flex items-center gap-2">
                <Switch
                  id={`active-${v.id}`}
                  checked={active}
                  disabled={cannotDeactivate || toggle.isPending}
                  onCheckedChange={(next) => toggle.mutate(next)}
                  aria-describedby={
                    cannotDeactivate ? `active-${v.id}-why` : undefined
                  }
                />
                <label
                  htmlFor={`active-${v.id}`}
                  className="font-semibold text-foreground"
                >
                  {active ? "Activo" : "Inactivo"}
                </label>
                {!active && <Badge variant="muted">Fuera del catálogo</Badge>}
              </span>
            )}
          </div>
          <AnimatePresence initial={false}>
            {cannotDeactivate && (
              <m.p
                id={`active-${v.id}-why`}
                initial={{ opacity: 0, height: 0 }}
                animate={{ opacity: 1, height: "auto" }}
                exit={{ opacity: 0, height: 0 }}
                className="text-[12.5px] text-muted-foreground"
              >
                Tiene reservas confirmadas o en curso: no se puede desactivar
                hasta que terminen.
              </m.p>
            )}
          </AnimatePresence>
        </div>
      </div>
    </m.li>
  );
}

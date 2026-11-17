import { useState } from "react";
import { m } from "motion/react";
import {
  CalendarDays,
  Car,
  CarFront,
  ChevronLeft,
  CreditCard,
  Hash,
  Search,
} from "lucide-react";
import { Link, useNavigate, useParams, useSearchParams } from "react-router";

import { errorCode, ERROR_CODES } from "@/api/errors";
import { PageError } from "@/components/custom/ErrorStates";
import { VehicleDetailSkeleton } from "@/components/custom/Skeletons";
import { NotFoundPage } from "@/features/errors/ErrorPages";
import EmptyState from "@/components/custom/EmptyState";
import { CarIllustration } from "@/components/custom/Illustrations";
import { Button, buttonVariants } from "@/components/ui/button";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import { useMediaQuery } from "@/hooks/useMediaQuery";
import { features } from "@/lib/features";
import { formatDayShort, formatMonthYear, formatPrice } from "@/lib/format";
import { ENTER } from "@/lib/motion";
import { isAdmin, useSession } from "@/stores/session.store";
import { approxPrice, vehicleName } from "../catalog/vehicle";
import { readSearch, searchQueryString } from "../search/params";
import { useBlockedDays } from "./availability";
import { AdminNotice, OwnVehicleNotice } from "./components/PanelNotices";
import ReservePanel, { type Range } from "./components/ReservePanel";
import VehicleGallery from "./components/VehicleGallery";
import VehicleReviews from "./components/VehicleReviews";
import { useVehicle } from "./useVehicle";

// Ficha del vehículo. Sin galería (la API no guarda fotos): ilustración de marca, datos y,
// a la derecha, el panel de reserva sticky. En móvil, barra inferior y sheet.
export default function VehiclePage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [params, setParams] = useSearchParams();
  const search = readSearch(params);
  const user = useSession((s) => s.user)!;
  const desktop = useMediaQuery("(min-width: 1024px)");
  const [sheetOpen, setSheetOpen] = useState(false);

  const { vehicle, isPending, isError, error, refetch, unreachable, rate } =
    useVehicle(id);
  const { blocked } = useBlockedDays(vehicle?.id);

  const range: Range | null = search.hasDates
    ? { from: search.from, to: search.to }
    : null;
  const setRange = (r: Range) => {
    const next = new URLSearchParams(params);
    next.set("desde", r.from);
    next.set("hasta", r.to);
    setParams(next, { replace: true, state: { rate, vehicle } });
  };
  const request = (r: Range) =>
    navigate(
      `/reservar/${vehicle!.id}${searchQueryString({ ...search, hasDates: true, from: r.from, to: r.to })}`,
      { state: { rate, vehicle } },
    );

  if (unreachable)
    return (
      <Centered>
        <EmptyState
          illustration={<CarIllustration className="w-37.5" />}
          title="Busca fechas para ver este vehículo"
          text="Abre la ficha desde los resultados de búsqueda."
          action={
            <Link
              to="/buscar"
              className={buttonVariants({
                size: "lg",
                className: "rounded-full",
              })}
            >
              <Search /> Ir al catálogo
            </Link>
          }
        />
      </Centered>
    );

  if (!vehicle && isPending) return <VehicleDetailSkeleton />;

  if (!vehicle || isError) {
    // Inexistente (o ajeno): 404, sin revelar nada. Otro fallo: error de página.
    if (errorCode(error) === ERROR_CODES.vehicleNotFound)
      return <NotFoundPage />;
    return <PageError error={error} onRetry={() => refetch()} />;
  }

  const own = vehicle.ownerId === user.id;
  const admin = isAdmin(user);
  const canReserve = !own && !admin && vehicle.isActive;
  const name = vehicleName(vehicle);

  const facts = [
    { icon: Car, label: "Marca", value: vehicle.brand },
    { icon: CarFront, label: "Modelo", value: vehicle.model },
    { icon: CalendarDays, label: "Año", value: String(vehicle.year) },
    // La placa solo la ve el dueño.
    ...(own ? [{ icon: Hash, label: "Placa", value: vehicle.plate }] : []),
  ];

  const panel = own ? (
    <OwnVehicleNotice />
  ) : admin ? (
    <AdminNotice />
  ) : !vehicle.isActive ? (
    <p className="text-[14.5px] text-muted-foreground">
      El anfitrión pausó este vehículo. No acepta reservas por ahora.
    </p>
  ) : (
    <ReservePanel
      vehicle={vehicle}
      range={range}
      onRange={setRange}
      rate={rate}
      blocked={blocked}
      onRequest={request}
    />
  );

  return (
    <main className="px-gutter pt-4 pb-32 lg:pb-16">
      <div className="mx-auto max-w-280">
        <Link
          to={`/buscar${searchQueryString(search)}`}
          className="mb-4 inline-flex items-center gap-1 rounded-full py-1 pr-2 text-[13.5px] font-semibold text-muted-foreground outline-none hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring"
        >
          <ChevronLeft className="size-4" /> Resultados
        </Link>

        <div className="grid gap-10 lg:grid-cols-[minmax(0,1fr)_380px] lg:gap-14">
          <m.div {...ENTER} className="grid min-w-0 content-start gap-8">
            <VehicleGallery vehicle={vehicle} />

            <header>
              <h1 className="text-[26px] leading-tight font-bold tracking-tight md:text-[30px]">
                {name}, {vehicle.year}
              </h1>
              <p className="mt-1 text-[14.5px] text-muted-foreground first-letter:uppercase">
                Publicado en {formatMonthYear(vehicle.createdAt)}
              </p>
            </header>

            <dl className="grid grid-cols-2 gap-3 border-y border-border-soft py-6 sm:grid-cols-4">
              {facts.map((f) => (
                <div
                  key={f.label}
                  className="flex items-center gap-3 rounded-2xl bg-secondary px-4 py-3"
                >
                  <f.icon
                    className="size-5 shrink-0 text-primary"
                    strokeWidth={1.5}
                  />
                  <div className="min-w-0">
                    <dt className="text-[12px] text-muted-foreground">
                      {f.label}
                    </dt>
                    <dd className="truncate text-[14.5px] font-semibold">
                      {f.value}
                    </dd>
                  </div>
                </div>
              ))}
            </dl>

            <section className="grid gap-3 text-[14.5px]">
              <h2 className="text-xl font-bold tracking-tight">
                Cómo funciona
              </h2>
              <p className="flex gap-3 text-muted-foreground">
                <CreditCard
                  className="mt-0.5 size-5 shrink-0 text-foreground"
                  strokeWidth={1.5}
                />
                Envías una solicitud con tus fechas. El anfitrión la confirma o
                la rechaza, y no se cobra nada en línea. El precio se acuerda en
                pesos dominicanos (DOP).
              </p>
            </section>

            {features.reviews && <VehicleReviews vehicleId={vehicle.id} />}
          </m.div>

          {desktop && (
            <aside className="self-start lg:sticky lg:top-28">
              <m.div
                {...ENTER}
                transition={{ ...ENTER.transition, delay: 0.08 }}
                className="rounded-[20px] border border-border bg-background p-6 shadow-float"
              >
                {panel}
              </m.div>
            </aside>
          )}
        </div>
      </div>

      {!desktop && (
        <>
          <div className="fixed inset-x-0 bottom-0 z-30 border-t border-border-soft bg-background/95 px-gutter pt-3 pb-safe backdrop-blur">
            <div className="flex items-center justify-between gap-4">
              <div className="min-w-0 text-[14px]">
                <p>
                  <strong className="text-base">
                    {formatPrice(vehicle.pricePerDay)}
                  </strong>{" "}
                  por día
                </p>
                <p className="truncate text-[12.5px] text-muted-foreground">
                  {range
                    ? `${formatDayShort(range.from)} – ${formatDayShort(range.to)}`
                    : (approxPrice(vehicle.pricePerDay, rate) ??
                      "Elige tus fechas")}
                </p>
              </div>
              {canReserve || own || admin ? (
                <Button
                  size="lg"
                  className="rounded-full px-7"
                  onClick={() => setSheetOpen(true)}
                >
                  {canReserve ? "Reservar" : "Ver opciones"}
                </Button>
              ) : null}
            </div>
          </div>
          <Sheet open={sheetOpen} onOpenChange={setSheetOpen}>
            <SheetContent
              side="bottom"
              className="max-h-[92dvh] overflow-y-auto"
            >
              <SheetHeader>
                <SheetTitle>{name}</SheetTitle>
              </SheetHeader>
              <div className="px-6 py-5">{panel}</div>
            </SheetContent>
          </Sheet>
        </>
      )}
    </main>
  );
}

function Centered({ children }: { children: React.ReactNode }) {
  return <main className="mx-auto max-w-160 px-gutter py-12">{children}</main>;
}

import { m } from "motion/react";
import { CircleAlert } from "lucide-react";
import { cn } from "cn";
import { Link, useSearchParams } from "react-router";

import { errorMessage, toApiError } from "@/api/errors";
import EmptyState from "@/components/custom/EmptyState";
import { CarIllustration } from "@/components/custom/Illustrations";
import Pagination from "@/components/custom/Pagination";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { buttonVariants } from "@/components/ui/button";
import { formatDateRange } from "@/lib/format";
import { ENTER, STAGGER_LIST } from "@/lib/motion";
import { parseCatalogParams } from "@/lib/schemas";
import ReserveDialog from "./components/ReserveDialog";
import TrustSection from "./components/TrustSection";
import VehicleCard from "./components/VehicleCard";
import { useAvailableVehicles } from "./hooks/useAvailableVehicles";

const GRID =
  "grid grid-cols-1 gap-x-5 gap-y-8 min-[480px]:grid-cols-2 min-[780px]:grid-cols-3 min-[1024px]:grid-cols-4 min-[1440px]:grid-cols-5 min-[1680px]:grid-cols-6";

// Catálogo: vehículos libres en las fechas de la URL (?desde=&hasta=&moneda=&pagina=). La
// reserva se abre con ?reservar=<id>, así que también se puede enlazar.
export default function CatalogPage() {
  const [params, setParams] = useSearchParams();
  const search = parseCatalogParams(params);
  const { data, isPending, isError, error, isPlaceholderData } =
    useAvailableVehicles(search);

  const reserveId = params.get("reservar");
  const reserving = data?.items.find((v) => v.id === reserveId) ?? null;

  const withParam = (key: string, value: string | null) => {
    const next = new URLSearchParams(params);
    if (value === null) next.delete(key);
    else next.set(key, value);
    return next;
  };
  const reserveHref = (id: string) => `/?${withParam("reservar", id)}`;
  const closeReserve = () =>
    setParams(withParam("reservar", null), { replace: true });

  // 503: la API de tasa de cambio no respondió. Los precios en DOP siguen disponibles.
  const conversionFailed = isError && toApiError(error).status === 503;

  return (
    <main className="px-gutter pt-6 pb-10">
      <m.header
        {...ENTER}
        className="mb-6 flex flex-wrap items-end justify-between gap-3"
      >
        <div>
          <h1 className="text-[22px] leading-tight font-bold tracking-tight md:text-[26px]">
            Vehículos disponibles
          </h1>
          <p className="mt-1 text-[14.5px] text-muted-foreground">
            {search.custom ? "Libres del " : "Libres los próximos días: "}
            {formatDateRange(search.from, search.to)}
            {search.currency !== "DOP" && ` · Precios en ${search.currency}`}
          </p>
        </div>
        {data && (
          <p className="text-[13.5px] text-muted-foreground" aria-live="polite">
            {data.totalItems} {data.totalItems === 1 ? "vehículo" : "vehículos"}
          </p>
        )}
      </m.header>

      {isPending ? (
        <ul className={GRID} aria-busy="true" aria-label="Cargando vehículos">
          {Array.from({ length: 8 }, (_, i) => (
            <li key={i} className="animate-pulse">
              <div className="aspect-4/3 rounded-2xl bg-secondary" />
              <div className="mt-3 h-3.5 w-2/3 rounded-full bg-secondary" />
              <div className="mt-2 h-3 w-1/2 rounded-full bg-secondary" />
            </li>
          ))}
        </ul>
      ) : isError ? (
        <Alert
          variant={conversionFailed ? "highlight" : "destructive"}
          className="max-w-160"
        >
          <CircleAlert />
          <AlertDescription>
            {conversionFailed
              ? "Ahora no podemos convertir los precios. Puedes verlos en pesos dominicanos."
              : errorMessage(error)}
            {conversionFailed && (
              <Link
                to={`/?${withParam("moneda", null)}`}
                className={cn(
                  buttonVariants({ variant: "outline", size: "sm" }),
                  "mt-3",
                )}
              >
                Ver precios en DOP
              </Link>
            )}
          </AlertDescription>
        </Alert>
      ) : data.items.length === 0 ? (
        <EmptyState
          illustration={<CarIllustration className="w-37.5" />}
          title="No hay vehículos libres en esas fechas"
          text="Prueba con otras fechas: los anfitriones publican vehículos nuevos a menudo."
        />
      ) : (
        <>
          <m.ul
            key={`${search.from}-${search.to}-${search.currency}-${search.page}`}
            {...STAGGER_LIST}
            className={GRID}
            style={{ opacity: isPlaceholderData ? 0.6 : 1 }}
          >
            {data.items.map((v) => (
              <VehicleCard key={v.id} vehicle={v} to={reserveHref(v.id)} />
            ))}
          </m.ul>
          <Pagination
            page={data.page}
            totalPages={data.totalPages}
            className="mt-10"
          />
        </>
      )}

      <TrustSection />

      <ReserveDialog
        vehicle={reserving}
        from={search.from}
        to={search.to}
        onClose={closeReserve}
      />
    </main>
  );
}

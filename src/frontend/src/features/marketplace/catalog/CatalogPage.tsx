import { m } from "motion/react";
import { CircleAlert, Info } from "lucide-react";
import { useSearchParams } from "react-router";

import { errorMessage } from "@/api/errors";
import EmptyState from "@/components/custom/EmptyState";
import { CarIllustration } from "@/components/custom/Illustrations";
import Pagination from "@/components/custom/Pagination";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { formatDateRange } from "@/lib/format";
import { ENTER, STAGGER_LIST } from "@/lib/motion";
import { daysBetween, parseCatalogParams } from "@/lib/schemas";
import { vehicleHref } from "../search/params";
import TrustSection from "./components/TrustSection";
import VehicleCard from "./components/VehicleCard";
import { useAvailableVehicles } from "./hooks/useAvailableVehicles";

const GRID =
  "grid grid-cols-1 gap-x-5 gap-y-8 min-[480px]:grid-cols-2 min-[780px]:grid-cols-3 min-[1024px]:grid-cols-4 min-[1440px]:grid-cols-5 min-[1680px]:grid-cols-6";

// Catálogo: vehículos libres en las fechas de la URL (?desde=&hasta=&moneda=&pagina=). Cada
// tarjeta abre la ficha del vehículo con la misma búsqueda.
export default function CatalogPage() {
  const [params] = useSearchParams();
  const search = parseCatalogParams(params);
  const { data, isPending, isError, error, isPlaceholderData } =
    useAvailableVehicles(search);
  const linkSearch = { ...search, hasDates: search.custom };

  // Se pidió otra moneda y ningún precio vino convertido: la tasa no estuvo disponible.
  const conversionFailed =
    search.currency !== "DOP" &&
    (data?.items.length ?? 0) > 0 &&
    data!.items.every((v) => v.convertedPricePerDay === null);

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
        <Alert variant="destructive" className="max-w-160">
          <CircleAlert />
          <AlertDescription>{errorMessage(error)}</AlertDescription>
        </Alert>
      ) : data.items.length === 0 ? (
        <EmptyState
          illustration={<CarIllustration className="w-37.5" />}
          title="No hay vehículos libres en esas fechas"
          text="Prueba con otras fechas: los anfitriones publican vehículos nuevos a menudo."
        />
      ) : (
        <>
          {conversionFailed && (
            <Alert variant="highlight" className="mb-6 max-w-160">
              <Info />
              <AlertDescription>
                Ahora no podemos convertir a {search.currency}. Mostramos los
                precios en DOP, que es como se cobra.
              </AlertDescription>
            </Alert>
          )}
          <m.ul
            key={`${search.from}-${search.to}-${search.currency}-${search.page}`}
            {...STAGGER_LIST}
            className={GRID}
            style={{ opacity: isPlaceholderData ? 0.6 : 1 }}
          >
            {data.items.map((v) => (
              <VehicleCard
                key={v.id}
                vehicle={v}
                to={vehicleHref(v.id, linkSearch)}
                days={
                  search.custom
                    ? daysBetween(search.from, search.to)
                    : undefined
                }
              />
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
    </main>
  );
}

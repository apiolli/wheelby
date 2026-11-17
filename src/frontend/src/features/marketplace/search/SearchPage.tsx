import { useState } from "react";
import { m } from "motion/react";
import { CircleAlert, Info, LoaderCircle, RotateCw } from "lucide-react";
import { useSearchParams } from "react-router";

import { errorMessage } from "@/api/errors";
import EmptyState from "@/components/custom/EmptyState";
import { CarIllustration } from "@/components/custom/Illustrations";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import VehicleCard from "@/features/marketplace/catalog/components/VehicleCard";
import { formatRangeWords } from "@/lib/format";
import { ENTER, STAGGER_LIST } from "@/lib/motion";
import type { Currency } from "@/lib/schemas";
import FilterRow from "./components/FilterRow";
import { readSearch, vehicleHref } from "./params";
import { useVehicleSearch } from "./useVehicleSearch";

export const RESULTS_GRID =
  "grid grid-cols-1 gap-x-5 gap-y-8 sm:grid-cols-2 lg:grid-cols-4";

// Resultados de búsqueda: vehículos libres en las fechas de la URL. Filtros en la URL
// (?desde=&hasta=&moneda=) para poder enlazar y volver atrás.
export default function SearchPage() {
  const [params, setParams] = useSearchParams();
  const search = readSearch(params);
  const results = useVehicleSearch(search);
  const [datesOpen, setDatesOpen] = useState(false);

  const update = (changes: Record<string, string | null>) => {
    const next = new URLSearchParams(params);
    for (const [key, value] of Object.entries(changes))
      if (value === null) next.delete(key);
      else next.set(key, value);
    setParams(next);
  };
  const setCurrency = (c: Currency) =>
    update({ moneda: c === "DOP" ? null : c });

  const range = formatRangeWords(search.from, search.to);

  return (
    <main className="px-gutter pt-6 pb-14">
      <m.header {...ENTER} className="mb-6 grid gap-4">
        <h1 className="sr-only">Resultados de búsqueda</h1>
        <FilterRow
          search={search}
          onDates={(r) => update({ desde: r.from, hasta: r.to })}
          onClearDates={() => update({ desde: null, hasta: null })}
          onCurrency={setCurrency}
          datesOpen={datesOpen}
          onDatesOpenChange={setDatesOpen}
        />
        <p className="text-[15px] font-semibold" aria-live="polite">
          {results.isPending ? (
            <span className="inline-block h-4 w-64 animate-pulse rounded-full bg-secondary align-middle" />
          ) : results.isError ? null : (
            <>
              {results.total}{" "}
              {results.total === 1
                ? "vehículo disponible"
                : "vehículos disponibles"}{" "}
              {range}
              {!search.hasDates && (
                <span className="font-normal text-muted-foreground">
                  {" "}
                  · próximos días
                </span>
              )}
            </>
          )}
        </p>
        {results.conversionFailed && (
          <m.div {...ENTER}>
            <Alert variant="highlight" className="max-w-160">
              <Info />
              <AlertDescription>
                Ahora no podemos convertir a {search.currency}. Mostramos los
                precios en DOP, que es como se cobra.
              </AlertDescription>
            </Alert>
          </m.div>
        )}
      </m.header>

      {results.isPending ? (
        <ResultsSkeleton />
      ) : results.isError ? (
        <Alert variant="destructive" className="max-w-160">
          <CircleAlert />
          <AlertDescription>
            {errorMessage(results.error)}
            <Button
              variant="outline"
              size="sm"
              className="mt-3 rounded-full"
              onClick={() => results.refetch()}
            >
              <RotateCw /> Reintentar
            </Button>
          </AlertDescription>
        </Alert>
      ) : results.items.length === 0 ? (
        <EmptyState
          illustration={<CarIllustration className="w-37.5" />}
          title="No hay vehículos disponibles en esas fechas."
          text="Prueba con otro rango: los anfitriones publican vehículos nuevos a menudo."
          action={
            <Button
              size="lg"
              className="rounded-full"
              onClick={() => setDatesOpen(true)}
            >
              Cambiar fechas
            </Button>
          }
        />
      ) : (
        <m.div
          initial={{ opacity: 0 }}
          animate={{ opacity: results.isPlaceholderData ? 0.6 : 1 }}
          transition={{ duration: 0.25 }}
        >
          <m.ul
            key={`${search.from}-${search.to}-${search.currency}`}
            {...STAGGER_LIST}
            className={RESULTS_GRID}
          >
            {results.items.map((v) => (
              <VehicleCard
                key={v.id}
                vehicle={v}
                to={vehicleHref(v.id, search)}
                days={search.hasDates ? search.days : undefined}
              />
            ))}
          </m.ul>
          {results.hasNextPage && (
            <div className="mt-12 flex flex-col items-center gap-2">
              <p className="text-[13px] text-muted-foreground">
                Mostrando {results.items.length} de {results.total}
              </p>
              <Button
                variant="outline"
                size="lg"
                className="min-w-44 rounded-full"
                disabled={results.isFetchingNextPage}
                aria-busy={results.isFetchingNextPage}
                onClick={() => results.fetchNextPage()}
              >
                {results.isFetchingNextPage ? (
                  <LoaderCircle
                    className="animate-spin"
                    aria-label="Cargando"
                  />
                ) : (
                  "Cargar más"
                )}
              </Button>
            </div>
          )}
        </m.div>
      )}
    </main>
  );
}

export function ResultsSkeleton() {
  return (
    <ul
      className={RESULTS_GRID}
      aria-busy="true"
      aria-label="Cargando vehículos"
    >
      {Array.from({ length: 8 }, (_, i) => (
        <li key={i} className="animate-pulse">
          <div className="aspect-4/3 rounded-[18px] bg-secondary" />
          <div className="mt-3 h-3.5 w-2/3 rounded-full bg-secondary" />
          <div className="mt-2 h-3 w-1/3 rounded-full bg-secondary" />
          <div className="mt-2 h-3 w-1/2 rounded-full bg-secondary" />
        </li>
      ))}
    </ul>
  );
}

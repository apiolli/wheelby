import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { m } from "motion/react";

import { listVehicleReviews } from "@/api/wheelby.api";
import EmptyState from "@/components/custom/EmptyState";
import { SectionError } from "@/components/custom/ErrorStates";
import { ReviewIllustration } from "@/components/custom/Illustrations";
import { Loading, Skeleton } from "@/components/custom/Skeletons";
import { Button } from "@/components/ui/button";
import { formatMonthYear } from "@/lib/format";
import { queryKeys } from "@/lib/query-keys";
import StarRating from "./StarRating";

// Se leen hasta 100 reseñas: alcanza para el promedio y la lista de la ficha.
const MAX_REVIEWS = 100;
const COLLAPSED = 4;

// P4: reseñas de arrendatarios sobre el vehículo, con promedio y lista.
export default function VehicleReviews({ vehicleId }: { vehicleId: string }) {
  const [expanded, setExpanded] = useState(false);
  const reviews = useQuery({
    queryKey: queryKeys.vehicleReviews(vehicleId),
    queryFn: () => listVehicleReviews(vehicleId, 1, MAX_REVIEWS),
  });

  if (reviews.isPending)
    return (
      <Loading label="Cargando reseñas" className="grid gap-3">
        <Skeleton className="h-6 w-48 rounded-full" />
        <div className="grid gap-4 sm:grid-cols-2">
          <Skeleton className="h-32 rounded-[18px]" />
          <Skeleton className="h-32 rounded-[18px]" />
        </div>
      </Loading>
    );
  if (reviews.isError)
    return (
      <section className="grid gap-4">
        <h2 className="text-xl font-bold tracking-tight">Reseñas</h2>
        <SectionError error={reviews.error} onRetry={() => reviews.refetch()} />
      </section>
    );

  const items = reviews.data.items;
  const average = items.length
    ? items.reduce((sum, r) => sum + r.rating, 0) / items.length
    : 0;
  const shown = expanded ? items : items.slice(0, COLLAPSED);

  return (
    <section aria-labelledby="reviews-title" className="grid gap-5">
      <header className="flex flex-wrap items-center gap-x-3 gap-y-1">
        <h2 id="reviews-title" className="text-xl font-bold tracking-tight">
          Reseñas
        </h2>
        {items.length > 0 && (
          <p className="flex items-center gap-2 text-[14.5px]">
            <StarRating value={average} size="md" />
            <strong>
              {average.toLocaleString("es-DO", {
                maximumFractionDigits: 1,
                minimumFractionDigits: 1,
              })}
            </strong>
            <span className="text-muted-foreground">
              · {reviews.data.totalItems}{" "}
              {reviews.data.totalItems === 1 ? "reseña" : "reseñas"}
            </span>
          </p>
        )}
      </header>

      {items.length === 0 ? (
        <EmptyState
          illustration={<ReviewIllustration className="w-32" />}
          title="Aún no hay reseñas para este vehículo."
          text="Las dejan los arrendatarios al terminar un alquiler."
        />
      ) : (
        <>
          <ul className="grid gap-4 sm:grid-cols-2">
            {shown.map((r, i) => (
              <m.li
                key={r.id}
                initial={{ opacity: 0, y: 8 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{
                  duration: 0.3,
                  delay: Math.max(0, i - COLLAPSED) * 0.04,
                }}
                className="rounded-[18px] border border-border-soft p-5"
              >
                <div className="flex items-center justify-between gap-3">
                  <StarRating value={r.rating} />
                  <span className="text-[12.5px] text-muted-foreground first-letter:uppercase">
                    {formatMonthYear(r.createdAt)}
                  </span>
                </div>
                <p className="mt-2.5 text-[14.5px] leading-relaxed">
                  {r.comment ?? (
                    <span className="text-muted-foreground">
                      Sin comentario.
                    </span>
                  )}
                </p>
                <p className="mt-3 text-[12.5px] font-semibold text-muted-foreground">
                  Arrendatario verificado
                </p>
              </m.li>
            ))}
          </ul>
          {items.length > COLLAPSED && (
            <Button
              variant="outline"
              className="w-fit rounded-full"
              onClick={() => setExpanded((e) => !e)}
            >
              {expanded
                ? "Mostrar menos"
                : `Mostrar las ${items.length} reseñas`}
            </Button>
          )}
        </>
      )}
    </section>
  );
}

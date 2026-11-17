import { useEffect, useState, type ReactNode } from "react";
import { m } from "motion/react";
import { cn } from "cn";

// Skeletons con la forma final del contenido (bloques #EBEBEB con un brillo sutil). Solo se
// muestran si la carga pasa de ~200 ms: una carga rápida no parpadea.

const DELAY_MS = 200;

export function Skeleton({ className }: { className?: string }) {
  return (
    <div aria-hidden="true" className={cn("skeleton rounded-xl", className)} />
  );
}

// Reserva el espacio desde el primer cuadro (sin saltos) y dibuja los bloques pasado el umbral.
export function Loading({
  label,
  children,
  className,
}: {
  label: string;
  children: ReactNode;
  className?: string;
}) {
  const [visible, setVisible] = useState(false);
  useEffect(() => {
    const timer = setTimeout(() => setVisible(true), DELAY_MS);
    return () => clearTimeout(timer);
  }, []);
  return (
    <div
      role="status"
      aria-busy="true"
      aria-label={label}
      className={className}
    >
      <div
        className={cn(
          "transition-opacity duration-200",
          visible ? "opacity-100" : "opacity-0",
        )}
      >
        {children}
      </div>
    </div>
  );
}

// El contenido que reemplaza a un skeleton entra con un fundido corto.
export function FadeIn({
  children,
  className,
}: {
  children: ReactNode;
  className?: string;
}) {
  return (
    <m.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      transition={{ duration: 0.25 }}
      className={className}
    >
      {children}
    </m.div>
  );
}

export function VehicleGridSkeleton({
  count = 8,
  className,
}: {
  count?: number;
  className: string;
}) {
  return (
    <Loading label="Cargando vehículos">
      <ul className={className}>
        {Array.from({ length: count }, (_, i) => (
          <li key={i}>
            <Skeleton className="aspect-4/3 rounded-[18px]" />
            <Skeleton className="mt-3 h-4 w-2/3 rounded-full" />
            <Skeleton className="mt-2 h-3 w-1/4 rounded-full" />
            <Skeleton className="mt-2.5 h-3.5 w-1/2 rounded-full" />
          </li>
        ))}
      </ul>
    </Loading>
  );
}

export function VehicleDetailSkeleton() {
  return (
    <Loading label="Cargando vehículo" className="px-gutter pt-12 pb-16">
      <div className="mx-auto grid max-w-280 gap-10 lg:grid-cols-[minmax(0,1fr)_380px] lg:gap-14">
        <div className="grid content-start gap-6">
          <Skeleton className="aspect-16/10 rounded-[20px]" />
          <Skeleton className="h-8 w-2/3 rounded-full" />
          <Skeleton className="h-4 w-1/3 rounded-full" />
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
            {[0, 1, 2].map((i) => (
              <Skeleton key={i} className="h-16 rounded-2xl" />
            ))}
          </div>
        </div>
        <Skeleton className="hidden h-96 rounded-[20px] lg:block" />
      </div>
    </Loading>
  );
}

export function ReservationListSkeleton({ count = 3 }: { count?: number }) {
  return (
    <Loading label="Cargando reservas">
      <ul className="grid grid-cols-1 gap-3">
        {Array.from({ length: count }, (_, i) => (
          <li
            key={i}
            className="flex gap-4 rounded-[18px] border border-border-soft p-3 sm:p-4"
          >
            <Skeleton className="aspect-4/3 w-24 shrink-0 rounded-xl sm:w-32" />
            <div className="grid flex-1 content-center gap-2">
              <Skeleton className="h-4 w-1/3 rounded-full" />
              <Skeleton className="h-3 w-1/2 rounded-full" />
              <Skeleton className="h-3 w-1/4 rounded-full" />
            </div>
            <Skeleton className="hidden h-6 w-20 self-center rounded-full sm:block" />
          </li>
        ))}
      </ul>
    </Loading>
  );
}

export function TimelineSkeleton() {
  return (
    <Loading label="Cargando línea de tiempo">
      <div className="grid gap-7">
        {[0, 1, 2, 3].map((i) => (
          <div key={i} className="flex items-center gap-4">
            <Skeleton className="size-6 shrink-0 rounded-full" />
            <div className="grid flex-1 gap-1.5">
              <Skeleton className="h-3.5 w-28 rounded-full" />
              <Skeleton className="h-3 w-40 rounded-full" />
            </div>
          </div>
        ))}
      </div>
    </Loading>
  );
}

export function TableSkeleton({
  rows = 6,
  columns = 5,
}: {
  rows?: number;
  columns?: number;
}) {
  return (
    <Loading label="Cargando tabla">
      <div className="grid">
        <div className="flex gap-6 border-b border-border-soft bg-secondary/60 px-4 py-3">
          {Array.from({ length: columns }, (_, c) => (
            <Skeleton key={c} className="h-3 flex-1 rounded-full" />
          ))}
        </div>
        {Array.from({ length: rows }, (_, r) => (
          <div
            key={r}
            className="flex items-center gap-6 border-b border-border-soft px-4 py-4 last:border-0"
          >
            {Array.from({ length: columns }, (_, c) => (
              <Skeleton
                key={c}
                className={cn(
                  "h-3.5 flex-1 rounded-full",
                  c === 0 && "max-w-24",
                )}
              />
            ))}
          </div>
        ))}
      </div>
    </Loading>
  );
}

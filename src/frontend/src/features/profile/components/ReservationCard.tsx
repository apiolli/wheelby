import type { ReactNode } from "react";
import { m } from "motion/react";
import { CalendarDays, Car } from "lucide-react";
import { cn } from "cn";

import { ReservationStatusBadge } from "@/components/custom/StatusBadges";
import { VEHICLE_ILLUSTRATION } from "@/features/marketplace/catalog/vehicle";
import { daysBetween } from "@/lib/schemas";
import { formatDate, formatDateRange, shortId } from "@/lib/format";
import { ReservationStatus, type Reservation } from "@/types/types";

interface ReservationCardProps {
  reservation: Reservation;
  actions?: ReactNode;
}

// Tarjeta horizontal: imagen pequeña, referencia, fechas y estado. La API solo devuelve el id
// del vehículo, así que se muestra su referencia corta.
export default function ReservationCard({
  reservation: r,
  actions,
}: ReservationCardProps) {
  const muted = r.status === ReservationStatus.Cancelled;
  const days = daysBetween(r.startDate.slice(0, 10), r.endDate.slice(0, 10));

  return (
    <m.li
      layout
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, scale: 0.97 }}
      transition={{ duration: 0.3 }}
      className="rounded-2xl border border-border p-3 sm:p-4"
    >
      <div className="flex gap-4 sm:items-center">
        <img
          src={VEHICLE_ILLUSTRATION}
          alt=""
          className={cn(
            "aspect-4/3 w-24 shrink-0 rounded-xl bg-secondary object-cover sm:w-32",
            muted && "opacity-60 grayscale",
          )}
        />
        <div className="flex min-w-0 flex-1 flex-col gap-2 sm:flex-row sm:items-center sm:justify-between sm:gap-4">
          <div className="min-w-0 text-sm">
            <p className="truncate text-[15px] font-bold">
              Reserva #{shortId(r.id)}
            </p>
            <p className="mt-1 flex items-center gap-1.5 text-muted-foreground">
              <Car className="size-3.5 shrink-0" strokeWidth={2} />
              <span className="truncate">Vehículo #{shortId(r.vehicleId)}</span>
            </p>
            <p className="mt-0.5 flex items-center gap-1.5 text-muted-foreground">
              <CalendarDays className="size-3.5 shrink-0" strokeWidth={2} />
              {formatDateRange(r.startDate, r.endDate)}
              {days > 0 && ` · ${days} ${days === 1 ? "día" : "días"}`}
            </p>
            <p className="mt-0.5 text-[12.5px] text-muted-foreground">
              Solicitada el {formatDate(r.createdAt)}
            </p>
          </div>
          <ReservationStatusBadge
            status={r.status}
            className="order-first self-start sm:order-none sm:self-center"
          />
        </div>
      </div>
      {actions && (
        <div className="mt-3 flex flex-wrap justify-end gap-2 border-t border-border-soft pt-3">
          {actions}
        </div>
      )}
    </m.li>
  );
}

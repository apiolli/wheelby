import type { ReactNode } from "react";
import { m } from "motion/react";
import { CalendarDays } from "lucide-react";
import { Link } from "react-router";

import { ReservationStatusBadge } from "@/components/custom/StatusBadges";
import VehicleArt from "@/components/custom/VehicleArt";
import { daysLabel, vehicleName } from "@/features/marketplace/catalog/vehicle";
import { features } from "@/lib/features";
import { formatDateRange, formatPrice } from "@/lib/format";
import { ReservationStatus, type Reservation } from "@/types/types";

interface ReservationCardProps {
  reservation: Reservation;
  actions?: ReactNode;
}

// Tarjeta horizontal: ilustración pequeña, vehículo, fechas, total y estado. Toda la tarjeta
// abre el detalle (P1); los botones de acción quedan por encima del enlace.
export default function ReservationCard({
  reservation: r,
  actions,
}: ReservationCardProps) {
  const muted = r.status === ReservationStatus.Cancelled;
  const name = vehicleName(r);

  return (
    <m.li
      layout
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, scale: 0.97 }}
      transition={{ duration: 0.3 }}
      className="group/card relative rounded-[18px] border border-border p-3 hover:border-foreground/40 sm:p-4"
    >
      <div className="flex gap-4 sm:items-center">
        <div className="aspect-4/3 w-24 shrink-0 overflow-hidden rounded-xl sm:w-32">
          <VehicleArt brand={r.brand} muted={muted} />
        </div>
        <div className="flex min-w-0 flex-1 flex-col gap-2 sm:flex-row sm:items-center sm:justify-between sm:gap-4">
          <div className="min-w-0 text-sm">
            <p className="truncate text-[15px] font-bold">
              {features.reservationDetail ? (
                <Link
                  to={`/reservas/${r.id}`}
                  className="outline-none after:absolute after:inset-0 after:rounded-[18px] focus-visible:after:ring-2 focus-visible:after:ring-ring"
                >
                  {name}
                </Link>
              ) : (
                name
              )}
              <span className="font-normal text-muted-foreground">
                {" "}
                · {r.year}
              </span>
            </p>
            <p className="mt-1 flex items-center gap-1.5 text-muted-foreground">
              <CalendarDays className="size-3.5 shrink-0" strokeWidth={2} />
              {formatDateRange(r.startDate, r.endDate)} · {daysLabel(r.days)}
            </p>
            <p className="mt-0.5 text-muted-foreground">
              Total{" "}
              <span className="font-semibold text-foreground">
                {formatPrice(r.totalAmount, r.currency)}
              </span>
            </p>
          </div>
          <ReservationStatusBadge
            status={r.status}
            className="order-first self-start sm:order-none sm:self-center"
          />
        </div>
      </div>
      {actions && (
        <div className="relative z-10 mt-3 flex flex-wrap justify-end gap-2 border-t border-border-soft pt-3">
          {actions}
        </div>
      )}
    </m.li>
  );
}

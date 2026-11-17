import { CalendarDays, UserRound } from "lucide-react";
import { Link } from "react-router";

import VehicleImage from "@/features/marketplace/vehicle/VehicleImage";
import { formatDateRange, formatPrice, shortId } from "@/lib/format";
import { ReservationStatus, type Reservation } from "@/types/types";
import { daysLabel, vehicleName } from "@/features/marketplace/catalog/vehicle";
import type { Side } from "../rules";

// Resumen: vehículo, fechas, total y contraparte. El precio quedó fijado al solicitar (P1), así
// que el total ya no es una estimación.
export default function SummaryCard({
  reservation: r,
  side,
}: {
  reservation: Reservation;
  side: Side;
}) {
  const muted = r.status === ReservationStatus.Cancelled;
  const counterpart =
    side === "renter"
      ? { label: "Anfitrión", id: r.ownerId }
      : side === "host"
        ? { label: "Arrendatario", id: r.renterId }
        : null;

  return (
    <section
      aria-label="Resumen de la reserva"
      className="grid gap-5 rounded-[20px] border border-border p-6 shadow-float"
    >
      <Link
        to={`/vehiculos/${r.vehicleId}`}
        className="group flex items-center gap-4 rounded-2xl outline-none focus-visible:ring-2 focus-visible:ring-ring"
      >
        <div className="aspect-4/3 w-28 shrink-0 overflow-hidden rounded-xl">
          <VehicleImage
            vehicleId={r.vehicleId}
            brand={r.brand}
            photoId={r.coverPhotoId}
            muted={muted}
          />
        </div>
        <div className="min-w-0">
          <p className="truncate text-[16px] font-bold group-hover:underline">
            {vehicleName(r)}
          </p>
          <p className="text-[14px] text-muted-foreground">
            {r.year}
            {side === "host" && ` · Placa ${r.plate}`}
          </p>
        </div>
      </Link>

      <dl className="grid gap-3 border-t border-border-soft pt-5 text-[14.5px]">
        <div className="flex items-center gap-3">
          <CalendarDays
            className="size-4.5 shrink-0 text-muted-foreground"
            strokeWidth={1.75}
          />
          <dt className="sr-only">Fechas</dt>
          <dd>
            {formatDateRange(r.startDate, r.endDate)} · {daysLabel(r.days)}
          </dd>
        </div>
        {counterpart && (
          <div className="flex items-center gap-3">
            <UserRound
              className="size-4.5 shrink-0 text-muted-foreground"
              strokeWidth={1.75}
            />
            <dt className="sr-only">{counterpart.label}</dt>
            <dd>
              {counterpart.label} · usuario #{shortId(counterpart.id)}
            </dd>
          </div>
        )}
      </dl>

      <dl className="grid gap-2.5 border-t border-border-soft pt-5 text-[14.5px]">
        <div className="flex justify-between gap-4">
          <dt className="text-muted-foreground">
            {formatPrice(r.pricePerDay)} × {daysLabel(r.days)}
          </dt>
          <dd className="tabular-nums">
            {formatPrice(r.pricePerDay * r.days)}
          </dd>
        </div>
        <div className="flex justify-between gap-4 border-t border-border-soft pt-3 text-base font-bold">
          <dt>Total</dt>
          <dd className="tabular-nums">
            {formatPrice(r.totalAmount, r.currency)}
          </dd>
        </div>
        <p className="text-[12.5px] text-muted-foreground">
          Precio fijado al solicitar. No se cobra nada en línea.
        </p>
      </dl>
    </section>
  );
}

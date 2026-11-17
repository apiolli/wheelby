import { m } from "motion/react";
import { CalendarDays } from "lucide-react";
import { Link } from "react-router";

import { STAGGER_ITEM } from "@/lib/motion";
import type { Vehicle } from "@/types/types";
import { VEHICLE_ILLUSTRATION, vehicleName, vehiclePrice } from "../vehicle";

interface VehicleCardProps {
  vehicle: Vehicle;
  // Enlace que abre la reserva (la misma URL con ?reservar=<id>).
  to: string;
}

// Tarjeta del catálogo: imagen 4:3 primero y el texto, secundario, debajo.
export default function VehicleCard({ vehicle, to }: VehicleCardProps) {
  const price = vehiclePrice(vehicle);
  const name = vehicleName(vehicle);

  return (
    <m.li {...STAGGER_ITEM} className="group/card min-w-0">
      <Link
        to={to}
        aria-label={`Reservar ${name}, ${price.main} por día`}
        className="block rounded-2xl outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-4"
      >
        <div className="relative aspect-4/3 overflow-hidden rounded-2xl bg-secondary">
          <img
            src={VEHICLE_ILLUSTRATION}
            alt=""
            className="size-full object-cover transition-transform duration-500 ease-out group-hover/card:scale-[1.03]"
          />
          <span className="absolute top-2.5 left-2.5 rounded-full bg-background/95 px-2.5 py-1 text-[11.5px] font-bold shadow-xs">
            {vehicle.year}
          </span>
        </div>

        <div className="pt-2.5 text-sm leading-snug">
          <h3 className="truncate text-sm font-bold">{name}</h3>
          <p className="mt-px truncate text-muted-foreground">
            Placa {vehicle.plate}
          </p>
          <p className="mt-px flex items-center gap-1 truncate text-muted-foreground">
            <CalendarDays className="size-3.5" strokeWidth={2} />
            Libre en tus fechas
          </p>
          <p className="mt-1.5">
            <strong className="font-bold">{price.main}</strong> por día
            {price.original && (
              <span className="ml-1 text-[12.5px] text-muted-foreground">
                ({price.original})
              </span>
            )}
          </p>
        </div>
      </Link>
    </m.li>
  );
}

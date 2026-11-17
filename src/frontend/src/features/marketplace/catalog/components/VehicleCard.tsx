import { m } from "motion/react";
import { Info } from "lucide-react";
import { Link } from "react-router";

import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import VehicleImage from "@/features/marketplace/vehicle/VehicleImage";
import { formatPrice } from "@/lib/format";
import { STAGGER_ITEM } from "@/lib/motion";
import type { Vehicle } from "@/types/types";
import { daysLabel, rateHint, vehicleName, vehiclePrice } from "../vehicle";

interface VehicleCardProps {
  vehicle: Vehicle;
  // Ficha del vehículo, con las fechas y la moneda de la búsqueda.
  to: string;
  // Días elegidos: si hay, se muestra el total estimado.
  days?: number;
  // Foto local para la vista previa del formulario de publicar.
  imageUrl?: string | null;
}

// Tarjeta del catálogo: ilustración 4:3 y, debajo, marca y modelo, año y precio. La placa no se
// muestra a otros usuarios. Toda la tarjeta es un enlace (el del título se estira encima), salvo
// el ícono de la tasa, que abre su tooltip.
export default function VehicleCard({
  vehicle,
  to,
  days,
  imageUrl,
}: VehicleCardProps) {
  const price = vehiclePrice(vehicle);
  const name = vehicleName(vehicle);

  return (
    <m.li {...STAGGER_ITEM} className="group/card relative min-w-0">
      <div className="relative aspect-4/3 overflow-hidden rounded-[18px] bg-secondary">
        <VehicleImage
          vehicleId={vehicle.id}
          brand={vehicle.brand}
          photoId={vehicle.coverPhotoId}
          src={imageUrl}
          className="transition-transform duration-500 ease-out group-hover/card:scale-[1.03]"
        />
        <span className="absolute top-2.5 left-2.5 rounded-full bg-background/95 px-2.5 py-1 text-[11.5px] font-bold shadow-xs">
          {vehicle.year}
        </span>
      </div>

      <div className="pt-2.5 text-sm leading-snug">
        <h3 className="truncate text-[15px] font-bold">
          <Link
            to={to}
            state={{ rate: rateHint(vehicle), vehicle }}
            className="outline-none after:absolute after:inset-0 after:rounded-[18px] focus-visible:after:ring-2 focus-visible:after:ring-ring focus-visible:after:ring-offset-4"
          >
            {name}
          </Link>
        </h3>
        <p className="mt-px text-muted-foreground">{vehicle.year}</p>
        <p className="mt-1.5 flex items-center gap-1">
          <strong className="font-bold">{price.approx ?? price.dop}</strong>
          <span>por día</span>
          {price.approx && (
            <Tooltip>
              <TooltipTrigger
                render={
                  <button
                    type="button"
                    aria-label={`Tasa de hoy. Se cobra en DOP: ${price.dop} por día.`}
                    className="relative z-10 grid size-6 place-items-center rounded-full text-muted-foreground outline-none hover:bg-secondary focus-visible:ring-2 focus-visible:ring-ring"
                  />
                }
              >
                <Info className="size-3.5" strokeWidth={2} />
              </TooltipTrigger>
              <TooltipContent>
                Tasa de hoy. Se cobra en DOP ({price.dop} por día).
              </TooltipContent>
            </Tooltip>
          )}
        </p>
        {days !== undefined && days > 0 && (
          <p className="mt-0.5 text-[13px] text-muted-foreground">
            Total estimado {formatPrice(vehicle.pricePerDay * days)} por{" "}
            {daysLabel(days)}
          </p>
        )}
      </div>
    </m.li>
  );
}

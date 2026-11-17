import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { cn } from "cn";

import { listVehiclePhotos } from "@/api/wheelby.api";
import VehicleArt from "@/components/custom/VehicleArt";
import { features } from "@/lib/features";
import { queryKeys } from "@/lib/query-keys";
import type { Vehicle } from "@/types/types";
import VehicleImage from "../VehicleImage";

// Galería de la ficha: foto principal 16:10 y miniaturas para cambiarla. Sin fotos, la
// ilustración de marca ocupa el mismo espacio.
export default function VehicleGallery({ vehicle }: { vehicle: Vehicle }) {
  const photos = useQuery({
    queryKey: queryKeys.vehiclePhotos(vehicle.id),
    queryFn: () => listVehiclePhotos(vehicle.id),
    enabled: features.vehiclePhotos,
  });
  const items = photos.data ?? [];
  const [selected, setSelected] = useState<string | null>(null);
  const current =
    items.find((p) => p.documentId === selected)?.documentId ??
    items[0]?.documentId ??
    vehicle.coverPhotoId;

  return (
    <div className="grid gap-3">
      <div className="aspect-16/10 overflow-hidden rounded-[20px] bg-secondary">
        {current ? (
          <VehicleImage
            vehicleId={vehicle.id}
            brand={vehicle.brand}
            photoId={current}
          />
        ) : (
          <VehicleArt brand={vehicle.brand} />
        )}
      </div>
      {items.length > 1 && (
        <ul
          className="-m-1 flex gap-2 overflow-x-auto p-1"
          aria-label="Fotos del vehículo"
        >
          {items.map((p, i) => {
            const active = p.documentId === current;
            return (
              <li key={p.documentId} className="shrink-0">
                <button
                  type="button"
                  aria-label={`Foto ${i + 1} de ${items.length}`}
                  aria-pressed={active}
                  onClick={() => setSelected(p.documentId)}
                  className={cn(
                    "block aspect-4/3 w-24 overflow-hidden rounded-xl outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 sm:w-28",
                    active
                      ? "ring-2 ring-foreground ring-offset-2"
                      : "opacity-75 hover:opacity-100",
                  )}
                >
                  <VehicleImage
                    vehicleId={vehicle.id}
                    brand={vehicle.brand}
                    photoId={p.documentId}
                  />
                </button>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}

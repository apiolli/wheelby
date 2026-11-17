import { m } from "motion/react";
import { cn } from "cn";

import VehicleArt from "@/components/custom/VehicleArt";
import { usePhoto } from "./usePhoto";

interface VehicleImageProps {
  vehicleId: string;
  brand: string;
  photoId?: string | null;
  // URL ya disponible (p. ej. una foto local en la vista previa del formulario).
  src?: string | null;
  muted?: boolean;
  className?: string;
  imgClassName?: string;
}

// Foto del vehículo sobre su ilustración de marca: mientras carga, si no hay foto o si falla, se
// ve la ilustración; la foto entra con un fundido cuando está lista.
export default function VehicleImage({
  vehicleId,
  brand,
  photoId,
  src,
  muted,
  className,
  imgClassName,
}: VehicleImageProps) {
  const photo = usePhoto(vehicleId, src ? null : photoId);
  const url = src ?? photo.data;

  return (
    <div className={cn("relative size-full", className)}>
      <VehicleArt brand={brand} muted={muted} />
      {url && (
        <m.img
          key={url}
          src={url}
          alt={`Foto de ${brand}`}
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ duration: 0.3 }}
          className={cn(
            "absolute inset-0 size-full object-cover",
            muted && "grayscale contrast-75",
            imgClassName,
          )}
        />
      )}
    </div>
  );
}

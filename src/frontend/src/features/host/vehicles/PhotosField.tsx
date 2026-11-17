import { useRef } from "react";
import { AnimatePresence, m } from "motion/react";
import { ImagePlus, LoaderCircle, RotateCw, Trash2 } from "lucide-react";
import { cn } from "cn";

import VehicleImage from "@/features/marketplace/vehicle/VehicleImage";
import { MAX_PHOTOS, PHOTO_ACCEPT } from "@/lib/schemas";
import type { PhotoItem } from "./usePhotoUploads";

interface PhotosFieldProps {
  vehicleId?: string;
  brand: string;
  items: PhotoItem[];
  isLoading: boolean;
  removing: string | null;
  onAdd: (files: FileList | null) => void;
  onRemove: (item: PhotoItem) => void;
  onRetry: (item: Extract<PhotoItem, { kind: "local" }>) => void;
  // Publicando: las fotos en cola aún no tienen vehículo.
  pendingPublish: boolean;
}

// Fotos del vehículo: cuadrícula de miniaturas, la primera es la portada. Cada foto local muestra
// su progreso o su error con reintento.
export default function PhotosField({
  vehicleId,
  brand,
  items,
  isLoading,
  removing,
  onAdd,
  onRemove,
  onRetry,
  pendingPublish,
}: PhotosFieldProps) {
  const input = useRef<HTMLInputElement>(null);
  const full = items.length >= MAX_PHOTOS;

  return (
    <div className="grid gap-3">
      <p className="text-[13px] text-muted-foreground">
        JPG, PNG o WebP · hasta 10 MB · máximo {MAX_PHOTOS}. La primera es la
        portada en el catálogo.
        {pendingPublish && " Se suben al publicar."}
      </p>
      {isLoading ? (
        <div className="h-28 animate-pulse rounded-2xl bg-secondary" />
      ) : (
        <ul
          className="grid grid-cols-2 gap-3 sm:grid-cols-3"
          aria-label="Fotos del vehículo"
        >
          <AnimatePresence initial={false}>
            {items.map((item, i) => {
              const local = item.kind === "local" ? item : null;
              const busy =
                local?.state === "uploading" ||
                (item.kind === "saved" && removing === item.documentId);
              return (
                <m.li
                  key={item.key}
                  layout
                  initial={{ opacity: 0, scale: 0.96 }}
                  animate={{ opacity: 1, scale: 1 }}
                  exit={{ opacity: 0, scale: 0.96 }}
                  transition={{ duration: 0.2 }}
                  className="relative aspect-4/3 overflow-hidden rounded-xl bg-secondary"
                >
                  {local ? (
                    <img
                      src={local.url}
                      alt={local.file.name}
                      className="size-full object-cover"
                    />
                  ) : (
                    <VehicleImage
                      vehicleId={vehicleId!}
                      brand={brand}
                      photoId={item.documentId}
                    />
                  )}
                  {i === 0 && (
                    <span className="absolute top-2 left-2 rounded-full bg-background/95 px-2 py-0.5 text-[11px] font-bold shadow-xs">
                      Portada
                    </span>
                  )}
                  {local?.state === "uploading" && (
                    <div className="absolute inset-x-2 bottom-2 grid gap-1 rounded-lg bg-background/95 p-2 text-[11.5px] font-semibold">
                      <span aria-live="polite">
                        {local.progress < 1
                          ? `Subiendo… ${Math.round(local.progress * 100)}%`
                          : "Guardando…"}
                      </span>
                      <span
                        role="progressbar"
                        aria-label={`Progreso de ${local.file.name}`}
                        aria-valuenow={Math.round(local.progress * 100)}
                        aria-valuemin={0}
                        aria-valuemax={100}
                        className="h-1 overflow-hidden rounded-full bg-border-soft"
                      >
                        <m.span
                          className="block h-full origin-left rounded-full bg-primary"
                          animate={{ scaleX: Math.max(0.03, local.progress) }}
                          transition={{ duration: 0.2 }}
                        />
                      </span>
                    </div>
                  )}
                  {local?.state === "error" && (
                    <div className="absolute inset-0 grid content-center justify-items-center gap-2 bg-destructive-soft/95 p-2 text-center text-[12px] text-destructive">
                      <span className="line-clamp-2">{local.error}</span>
                      {vehicleId && (
                        <button
                          type="button"
                          onClick={() => onRetry(local)}
                          className="inline-flex h-7 items-center gap-1 rounded-full border border-destructive bg-background px-2.5 font-semibold outline-none focus-visible:ring-2 focus-visible:ring-ring"
                        >
                          <RotateCw className="size-3.5" /> Reintentar
                        </button>
                      )}
                    </div>
                  )}
                  {!busy && (
                    <button
                      type="button"
                      aria-label={`Quitar foto ${i + 1}`}
                      onClick={() => onRemove(item)}
                      className="absolute top-2 right-2 grid size-8 place-items-center rounded-full bg-background/95 text-destructive shadow-xs outline-none hover:bg-destructive-soft focus-visible:ring-2 focus-visible:ring-ring"
                    >
                      <Trash2 className="size-4" />
                    </button>
                  )}
                  {busy && item.kind === "saved" && (
                    <div className="absolute inset-0 grid place-items-center bg-background/60">
                      <LoaderCircle
                        className="size-5 animate-spin"
                        aria-label="Quitando"
                      />
                    </div>
                  )}
                </m.li>
              );
            })}
          </AnimatePresence>
          {!full && (
            <li>
              <button
                type="button"
                onClick={() => input.current?.click()}
                className={cn(
                  "grid aspect-4/3 w-full place-items-center content-center gap-1.5 rounded-xl border-2 border-dashed border-border text-[13px] font-semibold text-muted-foreground outline-none",
                  "hover:border-foreground hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring",
                )}
              >
                <ImagePlus className="size-6" strokeWidth={1.5} />
                Agregar fotos
              </button>
              <input
                ref={input}
                type="file"
                multiple
                accept={PHOTO_ACCEPT}
                className="sr-only"
                tabIndex={-1}
                aria-label="Elegir fotos del vehículo"
                onChange={(e) => {
                  onAdd(e.target.files);
                  e.target.value = "";
                }}
              />
            </li>
          )}
        </ul>
      )}
    </div>
  );
}

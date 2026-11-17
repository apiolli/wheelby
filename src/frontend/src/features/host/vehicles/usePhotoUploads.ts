import { useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";

import { errorMessage } from "@/api/errors";
import {
  addVehiclePhoto,
  listVehiclePhotos,
  removeVehiclePhoto,
  uploadDocument,
} from "@/api/wheelby.api";
import { features } from "@/lib/features";
import { queryKeys } from "@/lib/query-keys";
import { MAX_PHOTOS, photoSchema } from "@/lib/schemas";
import { showToast } from "@/stores/session.store";

// Una foto del formulario: ya guardada en el vehículo, o local (en cola, subiendo o con error).
// documentId queda guardado si la subida terminó y lo que falló fue vincularla: el reintento no
// vuelve a subirla.
export type PhotoItem =
  | { key: string; kind: "saved"; documentId: string }
  | {
      key: string;
      kind: "local";
      file: File;
      url: string;
      state: "queued" | "uploading" | "error";
      progress: number;
      error?: string;
      documentId?: string;
    };

type LocalPhoto = Extract<PhotoItem, { kind: "local" }>;

// Fotos del formulario de vehículo (P4). Al publicar, las locales esperan a que exista el
// vehículo; al editar, se suben en cuanto se eligen.
export function usePhotoUploads(vehicleId: string | undefined) {
  const queryClient = useQueryClient();
  const saved = useQuery({
    queryKey: queryKeys.vehiclePhotos(vehicleId ?? ""),
    queryFn: () => listVehiclePhotos(vehicleId!),
    enabled: features.vehiclePhotos && Boolean(vehicleId),
  });
  const [local, setLocal] = useState<LocalPhoto[]>([]);
  const [removing, setRemoving] = useState<string | null>(null);

  const items: PhotoItem[] = [
    ...(saved.data ?? []).map((p) => ({
      key: p.documentId,
      kind: "saved" as const,
      documentId: p.documentId,
    })),
    ...local,
  ];

  const patch = (key: string, changes: Partial<LocalPhoto>) =>
    setLocal((list) =>
      list.map((p) => (p.key === key ? { ...p, ...changes } : p)),
    );

  const refresh = () =>
    Promise.all([
      queryClient.invalidateQueries({
        queryKey: queryKeys.vehiclePhotos(vehicleId ?? ""),
      }),
      queryClient.invalidateQueries({ queryKey: ["vehicles"] }),
    ]);

  // Sube y vincula una foto; devuelve false si falló (el error queda en la foto).
  const send = async (targetId: string, photo: LocalPhoto) => {
    patch(photo.key, { state: "uploading", error: undefined });
    try {
      const documentId =
        photo.documentId ??
        (
          await uploadDocument(photo.file, (p) =>
            patch(photo.key, { progress: p }),
          )
        ).id;
      patch(photo.key, { documentId, progress: 1 });
      await addVehiclePhoto(targetId, documentId);
      return true;
    } catch (err) {
      patch(photo.key, { state: "error", error: errorMessage(err) });
      return false;
    }
  };

  const finish = async (targetId: string, photo: LocalPhoto) => {
    if (await send(targetId, photo)) {
      await refresh();
      setLocal((list) => list.filter((p) => p.key !== photo.key));
      URL.revokeObjectURL(photo.url);
    }
  };

  const add = (files: FileList | null) => {
    const room = MAX_PHOTOS - items.length;
    const chosen = Array.from(files ?? []);
    if (chosen.length > room)
      showToast(`Puedes subir hasta ${MAX_PHOTOS} fotos por vehículo.`);
    for (const file of chosen.slice(0, Math.max(0, room))) {
      const check = photoSchema.safeParse(file);
      if (!check.success) {
        showToast(`${file.name}: ${check.error.issues[0].message}`);
        continue;
      }
      const photo: LocalPhoto = {
        key: crypto.randomUUID(),
        kind: "local",
        file,
        url: URL.createObjectURL(file),
        state: "queued",
        progress: 0,
      };
      setLocal((list) => [...list, photo]);
      if (vehicleId) void finish(vehicleId, photo);
    }
  };

  const retry = (photo: LocalPhoto) => {
    if (vehicleId) void finish(vehicleId, photo);
  };

  const remove = async (item: PhotoItem) => {
    if (item.kind === "local") {
      URL.revokeObjectURL(item.url);
      setLocal((list) => list.filter((p) => p.key !== item.key));
      return;
    }
    setRemoving(item.documentId);
    try {
      await removeVehiclePhoto(vehicleId!, item.documentId);
      await refresh();
    } catch (err) {
      showToast(errorMessage(err));
    } finally {
      setRemoving(null);
    }
  };

  // Tras publicar: sube las fotos en cola, una a una. Devuelve cuántas fallaron.
  const uploadQueued = async (targetId: string) => {
    let failed = 0;
    for (const photo of local.filter((p) => p.state !== "uploading"))
      if (!(await send(targetId, photo))) failed++;
    return failed;
  };

  return {
    items,
    isLoading: saved.isLoading,
    removing,
    add,
    retry,
    remove,
    uploadQueued,
    hasQueued: local.length > 0,
  };
}

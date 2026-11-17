import { useQuery } from "@tanstack/react-query";

import { fetchVehiclePhoto } from "@/api/wheelby.api";
import { features } from "@/lib/features";
import { queryKeys } from "@/lib/query-keys";

// URL local (blob) de una foto de vehículo. La API exige el token, así que un <img src> directo
// no sirve: se descarga una vez y se reutiliza mientras dure la sesión.
export function usePhoto(
  vehicleId: string,
  documentId: string | null | undefined,
) {
  return useQuery({
    queryKey: queryKeys.photo(documentId ?? ""),
    queryFn: async () =>
      URL.createObjectURL(await fetchVehiclePhoto(vehicleId, documentId!)),
    enabled: features.vehiclePhotos && Boolean(documentId),
    staleTime: Infinity,
    gcTime: 30 * 60_000,
    retry: 1,
  });
}

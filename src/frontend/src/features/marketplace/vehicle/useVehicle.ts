import { useQuery } from "@tanstack/react-query";
import { useLocation } from "react-router";

import { getVehicle } from "@/api/wheelby.api";
import { features } from "@/lib/features";
import { queryKeys } from "@/lib/query-keys";
import type { Vehicle } from "@/types/types";
import type { RateHint } from "../catalog/vehicle";

// Lo que la tarjeta de búsqueda deja en el estado de la navegación.
interface VehicleNavState {
  rate?: RateHint | null;
  vehicle?: Vehicle;
}

// Vehículo de la ficha: por URL directa con GET /vehicles/{id} (P1). Sin el endpoint, solo
// existe si se llegó desde un resultado de búsqueda (viene en el estado de la navegación).
export function useVehicle(id: string | undefined) {
  const state = (useLocation().state ?? {}) as VehicleNavState;
  const fromNav = state.vehicle?.id === id ? state.vehicle : undefined;

  const query = useQuery({
    queryKey: queryKeys.vehicle(id ?? ""),
    queryFn: () => getVehicle(id!),
    enabled: features.vehicleDetail && Boolean(id),
    // La tarjeta trae los mismos datos: se pintan al instante y se refrescan detrás.
    placeholderData: fromNav,
  });

  return {
    ...query,
    vehicle: query.data ?? fromNav,
    // Sin endpoint y sin datos de la búsqueda no hay forma de mostrarlo.
    unreachable: !features.vehicleDetail && !fromNav,
    rate: state.rate ?? null,
  };
}

import { useQuery } from "@tanstack/react-query";

import { listMyReservations } from "@/api/wheelby.api";
import { queryKeys } from "@/lib/query-keys";
import type { Reservation } from "@/types/types";

const PAGE_SIZE = 100;

// Todas las reservas de mis vehículos. La API no filtra por estado ni por vehículo, así que se
// leen completas (de 100 en 100) para contar por pestaña y por vehículo en el cliente.
export function useHostReservations() {
  return useQuery({
    queryKey: queryKeys.hostReservations,
    queryFn: async () => {
      const all: Reservation[] = [];
      for (let page = 1; ; page++) {
        const result = await listMyReservations("host", page, PAGE_SIZE);
        all.push(...result.items);
        if (!result.hasNextPage) return all;
      }
    },
  });
}

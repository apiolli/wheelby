import { useQuery } from "@tanstack/react-query";

import { getAvailability } from "@/api/wheelby.api";
import { features } from "@/lib/features";
import { queryKeys } from "@/lib/query-keys";
import { addDays, dayStartIso, today } from "@/lib/schemas";
import type { VehicleAvailability } from "@/types/types";

// Ventana que se consulta: desde hoy y un año hacia adelante (máximo del endpoint: 366 días).
const WINDOW_DAYS = 365;

// Días locales ocupados: los que se cruzan con una reserva Confirmed o InProgress. Una reserva
// que termina un día a medianoche deja ese día libre (puede empezar otra).
export function blockedDays(availability: VehicleAvailability | undefined) {
  const days = new Set<string>();
  for (const range of availability?.blocked ?? []) {
    const start = new Date(range.startDate).getTime();
    const end = new Date(range.endDate).getTime();
    let day = new Date(range.startDate).toLocaleDateString("en-CA");
    for (let i = 0; i < 400; i++) {
      const dayStart = new Date(`${day}T00:00:00`).getTime();
      if (dayStart >= end) break;
      if (new Date(`${addDays(day, 1)}T00:00:00`).getTime() > start)
        days.add(day);
      day = addDays(day, 1);
    }
  }
  return days;
}

// P1: si el endpoint no está activo, no hay días ocupados y el conflicto llega al enviar (409).
export function useBlockedDays(vehicleId: string | undefined) {
  const from = today();
  const to = addDays(from, WINDOW_DAYS);
  const query = useQuery({
    queryKey: queryKeys.availability(vehicleId ?? "", from, to),
    queryFn: () =>
      getAvailability(vehicleId!, dayStartIso(from), dayStartIso(to)),
    enabled: features.availability && Boolean(vehicleId),
    select: blockedDays,
    staleTime: 60_000,
  });
  return { blocked: query.data ?? EMPTY, isLoading: query.isLoading };
}

const EMPTY: ReadonlySet<string> = new Set();

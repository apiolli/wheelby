import { keepPreviousData, useInfiniteQuery } from "@tanstack/react-query";

import { searchAvailable } from "@/api/wheelby.api";
import { queryKeys } from "@/lib/query-keys";
import { dayStartIso } from "@/lib/schemas";
import type { SearchState } from "./params";

export const SEARCH_PAGE_SIZE = 24;

// Resultados con "Cargar más": cada página se agrega a las anteriores. Con otra moneda, la API
// añade el precio convertido (o lo deja en null si la tasa no está disponible).
export function useVehicleSearch({ from, to, currency }: SearchState) {
  const base = {
    from: dayStartIso(from),
    to: dayStartIso(to),
    pageSize: SEARCH_PAGE_SIZE,
    currency: currency === "DOP" ? undefined : currency,
  };
  const query = useInfiniteQuery({
    queryKey: queryKeys.available({ ...base, infinite: true }),
    queryFn: ({ pageParam }) => searchAvailable({ ...base, page: pageParam }),
    initialPageParam: 1,
    getNextPageParam: (last) => (last.hasNextPage ? last.page + 1 : undefined),
    placeholderData: keepPreviousData,
  });

  const items = query.data?.pages.flatMap((p) => p.items) ?? [];
  const total = query.data?.pages[0]?.totalItems ?? 0;
  // Se pidió otra moneda y ningún precio vino convertido: la tasa no estuvo disponible.
  const conversionFailed =
    currency !== "DOP" &&
    items.length > 0 &&
    items.every((v) => v.convertedPricePerDay === null);

  return { ...query, items, total, conversionFailed };
}

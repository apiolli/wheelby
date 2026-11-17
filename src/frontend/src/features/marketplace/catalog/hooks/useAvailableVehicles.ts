import { keepPreviousData, useQuery } from "@tanstack/react-query";

import { searchAvailable } from "@/api/wheelby.api";
import { queryKeys } from "@/lib/query-keys";
import { dayStartIso, type Currency } from "@/lib/schemas";

export const CATALOG_PAGE_SIZE = 24;

interface Params {
  from: string;
  to: string;
  currency: Currency;
  page: number;
}

// Vehículos libres entre dos días. Con otra moneda, la API añade el precio convertido.
export function useAvailableVehicles({ from, to, currency, page }: Params) {
  const query = {
    from: dayStartIso(from),
    to: dayStartIso(to),
    page,
    pageSize: CATALOG_PAGE_SIZE,
    currency: currency === "DOP" ? undefined : currency,
  };
  return useQuery({
    queryKey: queryKeys.available(query),
    queryFn: () => searchAvailable(query),
    // Al cambiar de página se mantiene la anterior hasta que llega la nueva.
    placeholderData: keepPreviousData,
  });
}

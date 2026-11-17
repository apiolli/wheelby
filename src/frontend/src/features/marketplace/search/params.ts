import { z } from "zod";

import {
  addDays,
  CURRENCIES,
  daysBetween,
  rangeSchema,
  today,
  type Currency,
} from "@/lib/schemas";

// Búsqueda en la URL: ?desde=2026-12-20&hasta=2026-12-23&moneda=USD. Todo es opcional: sin
// fechas se buscan los próximos días; un valor inválido se ignora en vez de mostrar un error.
export interface SearchState {
  from: string;
  to: string;
  // Fechas elegidas por la persona (no las de por defecto).
  hasDates: boolean;
  currency: Currency;
  days: number;
}

export function readSearch(params: URLSearchParams): SearchState {
  const range = rangeSchema.safeParse({
    from: params.get("desde"),
    to: params.get("hasta"),
  });
  const from = range.success ? range.data.from : addDays(today(), 1);
  const to = range.success ? range.data.to : addDays(today(), 4);
  return {
    from,
    to,
    hasDates: range.success,
    currency: z.enum(CURRENCIES).catch("DOP").parse(params.get("moneda")),
    days: daysBetween(from, to),
  };
}

// Mismos parámetros, para enlazar a la ficha o a la reserva sin perder la búsqueda.
export function searchQueryString(
  s: Pick<SearchState, "from" | "to" | "hasDates" | "currency">,
) {
  const params = new URLSearchParams();
  if (s.hasDates) {
    params.set("desde", s.from);
    params.set("hasta", s.to);
  }
  if (s.currency !== "DOP") params.set("moneda", s.currency);
  const text = params.toString();
  return text ? `?${text}` : "";
}

export const vehicleHref = (
  id: string,
  s: Parameters<typeof searchQueryString>[0],
) => `/vehiculos/${id}${searchQueryString(s)}`;

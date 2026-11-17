import {
  CalendarArrowDown,
  CalendarArrowUp,
  Coins,
  type LucideIcon,
} from "lucide-react";

import { formatDateRange } from "@/lib/format";
import type { Currency } from "@/lib/schemas";

// Búsqueda del catálogo: la API busca vehículos libres entre dos fechas y puede convertir los
// precios a otra moneda. Vive en la URL (?desde=&hasta=&moneda=) para poder compartirla.
export type SearchField = "from" | "to" | "currency";

export interface SearchQuery {
  from: string;
  to: string;
  currency: Currency;
}

export const SEARCH_FIELDS: {
  id: SearchField;
  label: string;
  icon: LucideIcon;
}[] = [
  { id: "from", label: "Recogida", icon: CalendarArrowUp },
  { id: "to", label: "Devolución", icon: CalendarArrowDown },
  { id: "currency", label: "Moneda", icon: Coins },
];

export const CURRENCY_LABEL: Record<Currency, string> = {
  DOP: "Peso dominicano (DOP)",
  USD: "Dólar (USD)",
  EUR: "Euro (EUR)",
};

export const toSearchParams = (q: SearchQuery) => {
  const params = new URLSearchParams({ desde: q.from, hasta: q.to });
  if (q.currency !== "DOP") params.set("moneda", q.currency);
  return params;
};

export const rangeLabel = (q: SearchQuery) => formatDateRange(q.from, q.to);

import { formatPrice } from "@/lib/format";
import type { Currency } from "@/lib/schemas";
import type { Vehicle } from "@/types/types";

export const vehicleName = (v: Pick<Vehicle, "brand" | "model">) =>
  `${v.brand} ${v.model}`;

// Precio por día: siempre en DOP (lo que se cobra) y, si la búsqueda pidió otra moneda y la API
// pudo convertir, el aproximado ("≈ US$45").
export function vehiclePrice(v: Vehicle) {
  const approx =
    v.convertedPricePerDay !== null && v.convertedCurrency
      ? `≈ ${formatPrice(v.convertedPricePerDay, v.convertedCurrency, { approx: true })}`
      : null;
  return { dop: formatPrice(v.pricePerDay), approx };
}

// Tasa que viaja de la búsqueda a la ficha (la ficha se lee siempre en DOP).
export interface RateHint {
  currency: Currency;
  rate: number;
}

export function rateHint(v: Vehicle): RateHint | null {
  if (v.exchangeRate === null || !v.convertedCurrency) return null;
  return { currency: v.convertedCurrency as Currency, rate: v.exchangeRate };
}

export const approxPrice = (dop: number, hint: RateHint | null) =>
  hint
    ? `≈ ${formatPrice(dop * hint.rate, hint.currency, { approx: true })}`
    : null;

// "3 días"
export const daysLabel = (n: number) => `${n} ${n === 1 ? "día" : "días"}`;

import { formatPrice } from "@/lib/format";
import type { Vehicle } from "@/types/types";

// La API no guarda fotos de los vehículos: cada tarjeta usa esta ilustración de marca.
const GLYPH =
  '<path d="M5 17v-4l2.2-5A2 2 0 0 1 9 7h6a2 2 0 0 1 1.8 1l2.2 5v4M4 17h16M5 13h14"/><circle cx="8" cy="17" r="2"/><circle cx="16" cy="17" r="2"/>';

export const VEHICLE_ILLUSTRATION =
  "data:image/svg+xml;utf8," +
  encodeURIComponent(
    `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 400 300"><defs><linearGradient id="g" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#E7F0EF"/><stop offset="1" stop-color="#CFE0DE"/></linearGradient></defs><rect width="400" height="300" fill="url(#g)"/><g transform="translate(140 90) scale(5)" fill="none" stroke="#0E4F4B" stroke-width="1" stroke-linecap="round" stroke-linejoin="round" opacity=".55">${GLYPH}</g></svg>`,
  );

export const vehicleName = (v: Pick<Vehicle, "brand" | "model">) =>
  `${v.brand} ${v.model}`;

// Precio principal en la moneda pedida (si la API lo convirtió) y, aparte, el original en DOP.
export function vehiclePrice(v: Vehicle) {
  const converted =
    v.convertedPricePerDay !== null && v.convertedCurrency
      ? formatPrice(v.convertedPricePerDay, v.convertedCurrency)
      : null;
  const original = formatPrice(v.pricePerDay, v.currency);
  return { main: converted ?? original, original: converted ? original : null };
}

// Formatos de fecha, precio y texto compartidos. Acepta días (YYYY-MM-DD, en hora local) o
// fechas completas de la API (ISO en UTC), que se muestran en la hora local.

const parse = (iso: string) =>
  iso.length > 10 ? new Date(iso) : new Date(`${iso}T12:00:00`);

const SHORT_MONTH = new Intl.DateTimeFormat("es-DO", { month: "short" });
const MONTH_YEAR = new Intl.DateTimeFormat("es-DO", {
  month: "long",
  year: "numeric",
});

const month = (d: Date) => SHORT_MONTH.format(d).replace(".", "");

// "14 mar 2025"
export function formatDate(iso: string) {
  const d = parse(iso);
  return `${d.getDate()} ${month(d)} ${d.getFullYear()}`;
}

// "marzo de 2025"
export const formatMonthYear = (iso: string) => MONTH_YEAR.format(parse(iso));

// "12 – 15 oct 2026", o "28 sept – 2 oct 2026" si cambia de mes.
export function formatDateRange(start: string, end: string) {
  const a = parse(start);
  const b = parse(end);
  if (a.getFullYear() !== b.getFullYear())
    return `${formatDate(start)} – ${formatDate(end)}`;
  if (a.getMonth() === b.getMonth())
    return `${a.getDate()} – ${formatDate(end)}`;
  return `${a.getDate()} ${month(a)} – ${formatDate(end)}`;
}

// "del 20 al 23 dic" · "del 28 nov al 2 dic" · con año si no es el actual.
export function formatRangeWords(start: string, end: string) {
  const a = parse(start);
  const b = parse(end);
  const year = (d: Date) =>
    d.getFullYear() !== new Date().getFullYear() ? ` ${d.getFullYear()}` : "";
  const first =
    a.getMonth() === b.getMonth() && a.getFullYear() === b.getFullYear()
      ? `${a.getDate()}`
      : `${a.getDate()} ${month(a)}${a.getFullYear() !== b.getFullYear() ? year(a) : ""}`;
  return `del ${first} al ${b.getDate()} ${month(b)}${year(b)}`;
}

// "20 nov" (o "20 nov 2027" si no es el año actual).
export function formatDayShort(iso: string) {
  const d = parse(iso);
  const y =
    d.getFullYear() !== new Date().getFullYear() ? ` ${d.getFullYear()}` : "";
  return `${d.getDate()} ${month(d)}${y}`;
}

export const firstName = (name: string) => name.trim().split(/\s+/)[0];

export const initial = (name: string) => name.trim().charAt(0).toUpperCase();

// "RD$4,500" · "US$45.50" · "€41.20". DOP sin decimales; otras monedas con dos, salvo que se
// pida un aproximado ("≈ US$45"), que va redondeado.
export function formatPrice(
  amount: number,
  currency = "DOP",
  { approx = false }: { approx?: boolean } = {},
) {
  const decimals = currency === "DOP" || approx ? 0 : 2;
  return new Intl.NumberFormat("es-DO", {
    style: "currency",
    currency,
    // es-DO escribe "RD$" y "US$"; para el euro, "€" en vez de "EUR".
    currencyDisplay: currency === "EUR" ? "narrowSymbol" : "symbol",
    maximumFractionDigits: decimals,
    minimumFractionDigits: decimals,
  }).format(amount);
}

// "2.4 MB"
export function formatBytes(bytes: number) {
  if (bytes < 1024) return `${bytes} B`;
  const units = ["KB", "MB", "GB"];
  let value = bytes / 1024;
  let i = 0;
  while (value >= 1024 && i < units.length - 1) {
    value /= 1024;
    i++;
  }
  return `${value.toFixed(value < 10 ? 1 : 0)} ${units[i]}`;
}

// Referencia corta y legible de un id (los 6 primeros caracteres, en mayúsculas).
export const shortId = (id: string) => id.slice(0, 6).toUpperCase();

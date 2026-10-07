// Formatos de fecha y texto compartidos. Fechas ISO (YYYY-MM-DD) en hora local.

const parse = (iso: string) => new Date(`${iso.slice(0, 10)}T12:00:00`);

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

export const firstName = (name: string) => name.trim().split(/\s+/)[0];

export const initial = (name: string) => name.trim().charAt(0).toUpperCase();

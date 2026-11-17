import { addDays } from "./schemas";

// ¿Hay algún día ocupado en [from, to)? El día de devolución puede ser el inicio de otra
// reserva, así que no cuenta.
export function hasBlockedBetween(
  blocked: ReadonlySet<string>,
  from: string,
  to: string,
) {
  for (let d = from; d < to; d = addDays(d, 1)) if (blocked.has(d)) return true;
  return false;
}

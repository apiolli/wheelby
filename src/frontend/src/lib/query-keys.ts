import type { ReservationStatus } from "@/types/types";

// Claves de TanStack Query, en un solo lugar para invalidar sin errores de tecleo.
export const queryKeys = {
  available: (q: object) => ["vehicles", "available", q] as const,
  vehicle: (id: string) => ["vehicles", id] as const,
  availability: (id: string, from: string, to: string) =>
    ["vehicles", id, "availability", from, to] as const,
  vehicleReviews: (id: string) => ["vehicles", id, "reviews"] as const,
  // Todo lo de una reserva cuelga de esta clave: invalidarla refresca detalle, historial, etc.
  reservation: (id: string) => ["reservation", id] as const,
  reservationPart: (id: string, part: string) =>
    ["reservation", id, part] as const,
  myReservations: (as: string, page?: number) =>
    page === undefined
      ? (["reservations", "mine", as] as const)
      : (["reservations", "mine", as, page] as const),
  myRequests: ["permissions", "mine"] as const,
  documents: (page?: number) =>
    page === undefined
      ? (["documents"] as const)
      : (["documents", page] as const),
  adminUsers: (page?: number) =>
    page === undefined
      ? (["admin", "users"] as const)
      : (["admin", "users", page] as const),
  adminRequests: ["admin", "requests"] as const,
  adminReservations: (status?: ReservationStatus, page?: number) =>
    ["admin", "reservations", status ?? "all", page ?? 1] as const,
};

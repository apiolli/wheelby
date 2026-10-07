// Reservas de ejemplo del arrendatario (solo lectura en el perfil).
import { SECTIONS } from "./vehicle";

export type BookingStatus = "confirmed" | "pending" | "completed" | "cancelled";

export interface Booking {
  id: string;
  accountId: string;
  vehicleId: string;
  start: string;
  end: string;
  status: BookingStatus;
}

export const BOOKING_STATUS_LABEL: Record<BookingStatus, string> = {
  confirmed: "Confirmada",
  pending: "Pendiente",
  completed: "Completada",
  cancelled: "Cancelada",
};

const vehicle = (section: string, index: number) =>
  SECTIONS.find((s) => s.id === section)?.items[index].id ?? "";

export const BOOKINGS: Booking[] = [
  {
    id: "b-1",
    accountId: "u-demo",
    vehicleId: vehicle("playa", 1),
    start: "2026-10-16",
    end: "2026-10-19",
    status: "confirmed",
  },
  {
    id: "b-2",
    accountId: "u-demo",
    vehicleId: vehicle("motos-finde", 0),
    start: "2026-10-24",
    end: "2026-10-25",
    status: "pending",
  },
  {
    id: "b-3",
    accountId: "u-demo",
    vehicleId: vehicle("autos-santiago", 0),
    start: "2026-09-05",
    end: "2026-09-08",
    status: "completed",
  },
  {
    id: "b-4",
    accountId: "u-demo",
    vehicleId: vehicle("ligeros", 2),
    start: "2026-08-28",
    end: "2026-09-02",
    status: "cancelled",
  },
];

export const bookingsOf = (accountId: string) =>
  BOOKINGS.filter((b) => b.accountId === accountId);

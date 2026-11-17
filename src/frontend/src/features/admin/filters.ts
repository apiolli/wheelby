import { ReservationStatus, type Role, type UserStatus } from "@/types/types";

// Filtros en la URL con slugs en español (?rol=admin&estado=desactivada).
export interface FilterOption<T> {
  slug: string;
  label: string;
  value?: T;
}

export const ROLE_FILTERS: FilterOption<Role>[] = [
  { slug: "todos", label: "Todos los roles" },
  { slug: "estandar", label: "Estándar", value: "Standard" },
  { slug: "admin", label: "Administrador", value: "Administrator" },
];

export const STATUS_FILTERS: FilterOption<UserStatus>[] = [
  { slug: "todos", label: "Todos los estados" },
  { slug: "activa", label: "Activa", value: "Active" },
  { slug: "sin-activar", label: "Sin activar", value: "PendingActivation" },
  { slug: "desactivada", label: "Desactivada", value: "Disabled" },
];

export const RESERVATION_FILTERS: FilterOption<ReservationStatus>[] = [
  { slug: "todas", label: "Todas" },
  { slug: "solicitada", label: "Solicitada", value: ReservationStatus.Pending },
  {
    slug: "confirmada",
    label: "Confirmada",
    value: ReservationStatus.Confirmed,
  },
  { slug: "en-curso", label: "En curso", value: ReservationStatus.InProgress },
  {
    slug: "completada",
    label: "Completada",
    value: ReservationStatus.Completed,
  },
  {
    slug: "cancelada",
    label: "Cancelada",
    value: ReservationStatus.Cancelled,
  },
];

export const findFilter = <T>(
  options: FilterOption<T>[],
  slug: string | null,
) => options.find((f) => f.slug === slug) ?? options[0];

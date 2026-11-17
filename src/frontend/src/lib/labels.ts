import {
  ReservationStatus,
  type PermissionRequestStatus,
  type Role,
  type UserStatus,
} from "@/types/types";

// Textos y variantes de badge de los estados que devuelve la API.

export type Variant =
  | "success"
  | "highlight"
  | "destructive"
  | "muted"
  | "soft"
  | "primary"
  | "outline";

export const ROLE_LABEL: Record<Role, string> = {
  Administrator: "Administrador",
  Standard: "Estándar",
};

export const USER_STATUS_LABEL: Record<UserStatus, string> = {
  Active: "Activa",
  PendingActivation: "Sin activar",
  Disabled: "Desactivada",
};

export const USER_STATUS_VARIANT: Record<UserStatus, Variant> = {
  Active: "success",
  PendingActivation: "highlight",
  Disabled: "muted",
};

export const RESERVATION_STATUS_LABEL: Record<ReservationStatus, string> = {
  [ReservationStatus.Pending]: "Pendiente",
  [ReservationStatus.Confirmed]: "Confirmada",
  [ReservationStatus.InProgress]: "En curso",
  [ReservationStatus.Completed]: "Completada",
  [ReservationStatus.Cancelled]: "Cancelada",
};

export const RESERVATION_STATUS_VARIANT: Record<ReservationStatus, Variant> = {
  [ReservationStatus.Pending]: "highlight",
  [ReservationStatus.Confirmed]: "success",
  [ReservationStatus.InProgress]: "soft",
  [ReservationStatus.Completed]: "muted",
  [ReservationStatus.Cancelled]: "destructive",
};

export const REQUEST_STATUS_LABEL: Record<PermissionRequestStatus, string> = {
  Pending: "Pendiente",
  Approved: "Aprobada",
  Applied: "Aprobada",
  Rejected: "Rechazada",
};

export const REQUEST_STATUS_VARIANT: Record<PermissionRequestStatus, Variant> =
  {
    Pending: "highlight",
    Approved: "success",
    Applied: "success",
    Rejected: "destructive",
  };

// Tipos del dominio tal como los devuelve la API de Wheelby.

export type Role = "Administrator" | "Standard";
export type UserStatus = "Active" | "PendingActivation" | "Disabled";

// Aviso informativo (no es un error) que el login muestra sobre el formulario.
export type AuthNotice = "expired" | "password-reset" | "activated";

// Estado de navegación entre las páginas de acceso: correo precargado y aviso.
export interface AuthRouteState {
  email?: string;
  notice?: AuthNotice;
}

// GET /auth/me
export interface User {
  id: string;
  fullName: string;
  email: string;
  role: Role;
}

// POST /auth/login
export interface LoginResult {
  accessToken: string;
  expiresAt: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

// GET /admin/users
export interface UserSummary {
  id: string;
  fullName: string;
  email: string;
  role: Role;
  status: UserStatus;
  createdAt: string;
}

export type PermissionRequestStatus =
  | "Pending"
  | "Approved"
  | "Applied"
  | "Rejected";

// GET /permissions/requests/mine y /admin/permissions/requests
export interface PermissionRequest {
  id: string;
  requesterId: string;
  permissionCode: string;
  status: PermissionRequestStatus;
  approverId: string | null;
  createdAt: string;
  resolvedAt: string | null;
  rejectionReason: string | null;
}

// GET /vehicles/available. Precios en DOP; los campos converted* llegan si se pide otra moneda.
export interface Vehicle {
  id: string;
  ownerId: string;
  brand: string;
  model: string;
  year: number;
  plate: string;
  pricePerDay: number;
  createdAt: string;
  currency: string;
  convertedPricePerDay: number | null;
  convertedCurrency: string | null;
  exchangeRate: number | null;
}

// La API serializa el estado de la reserva como texto (JsonStringEnumConverter).
export const ReservationStatus = {
  Pending: "Pending",
  Confirmed: "Confirmed",
  InProgress: "InProgress",
  Completed: "Completed",
  Cancelled: "Cancelled",
} as const;
export type ReservationStatus =
  (typeof ReservationStatus)[keyof typeof ReservationStatus];

// GET /reservations/mine, /reservations/{id} y /admin/reservations.
// pricePerDay y totalAmount están congelados al reservar, en DOP.
export interface Reservation {
  id: string;
  vehicleId: string;
  renterId: string;
  ownerId: string;
  brand: string;
  model: string;
  year: number;
  plate: string;
  startDate: string;
  endDate: string;
  status: ReservationStatus;
  pricePerDay: number;
  days: number;
  totalAmount: number;
  cancellationReason: string | null;
  createdAt: string;
  currency: string;
}

// GET /vehicles/{id}/availability: rangos bloqueados (Confirmed e InProgress).
export interface VehicleAvailability {
  vehicleId: string;
  from: string;
  to: string;
  blocked: { startDate: string; endDate: string }[];
}

// GET /documents
export interface DocumentSummary {
  id: string;
  ownerId: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  uploadedAt: string;
}

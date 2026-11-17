import type {
  DocumentSummary,
  PagedResult,
  PermissionRequest,
  Reservation,
  ReservationDocument,
  ReservationStatusChange,
  Review,
  Vehicle,
  VehicleAvailability,
} from "@/types/types";
import { wheelbyApi } from "./wheelby-api";

// Endpoints del miembro: vehículos, reservas, permisos y documentos.

// ---------- Vehículos ----------

export interface AvailabilityQuery {
  from: string; // ISO
  to: string; // ISO
  page: number;
  pageSize: number;
  currency?: string;
}

export const searchAvailable = (q: AvailabilityQuery) =>
  wheelbyApi
    .get<PagedResult<Vehicle>>("/vehicles/available", { params: q })
    .then((r) => r.data);

// P1: ficha por URL directa. Siempre en DOP (la conversión solo viene en la búsqueda).
export const getVehicle = (id: string) =>
  wheelbyApi.get<Vehicle>(`/vehicles/${id}`).then((r) => r.data);

// P1: rangos bloqueados (Confirmed e InProgress) entre dos instantes.
export const getAvailability = (id: string, from: string, to: string) =>
  wheelbyApi
    .get<VehicleAvailability>(`/vehicles/${id}/availability`, {
      params: { from, to },
    })
    .then((r) => r.data);

// P4: reseñas de inquilinos sobre el vehículo.
export const listVehicleReviews = (
  id: string,
  page: number,
  pageSize: number,
) =>
  wheelbyApi
    .get<PagedResult<Review>>(`/vehicles/${id}/reviews`, {
      params: { page, pageSize },
    })
    .then((r) => r.data);

export interface PublishVehicleInput {
  brand: string;
  model: string;
  year: number;
  plate: string;
  pricePerDay: number;
}

export const publishVehicle = (input: PublishVehicleInput) =>
  wheelbyApi.post<{ id: string }>("/vehicles", input).then((r) => r.data);

// ---------- Reservas ----------

export type ReservationRole = "renter" | "host";

export const requestReservation = (
  vehicleId: string,
  start: string,
  end: string,
) =>
  wheelbyApi
    .post<{ id: string }>("/reservations", { vehicleId, start, end })
    .then((r) => r.data);

export const listMyReservations = (
  as: ReservationRole,
  page: number,
  pageSize: number,
) =>
  wheelbyApi
    .get<PagedResult<Reservation>>("/reservations/mine", {
      params: { as, page, pageSize },
    })
    .then((r) => r.data);

// El Administrador no entra al grupo /reservations: lee lo mismo bajo /admin/reservations.
const reservationBase = (id: string, admin: boolean) =>
  `${admin ? "/admin/reservations" : "/reservations"}/${id}`;

// P1: detalle (participantes y Administrador; para otro, 404).
export const getReservation = (id: string, admin = false) =>
  wheelbyApi.get<Reservation>(reservationBase(id, admin)).then((r) => r.data);

// P3: línea de tiempo de estados.
export const getReservationHistory = (id: string, admin = false) =>
  wheelbyApi
    .get<ReservationStatusChange[]>(`${reservationBase(id, admin)}/history`)
    .then((r) => r.data);

// P3: documentos de entrega y devolución.
export const listReservationDocuments = (id: string, admin = false) =>
  wheelbyApi
    .get<ReservationDocument[]>(`${reservationBase(id, admin)}/documents`)
    .then((r) => r.data);

export async function downloadReservationDocument(
  reservationId: string,
  doc: ReservationDocument,
  admin = false,
) {
  const { data } = await wheelbyApi.get<Blob>(
    `${reservationBase(reservationId, admin)}/documents/${doc.documentId}/content`,
    { responseType: "blob" },
  );
  saveBlob(data, doc.fileName ?? "documento");
}

// P4: reseñas de la reserva (las dos partes) y publicar la propia.
export const listReservationReviews = (id: string, admin = false) =>
  wheelbyApi
    .get<Review[]>(`${reservationBase(id, admin)}/reviews`)
    .then((r) => r.data);

export const createReview = (id: string, rating: number, comment: string) =>
  wheelbyApi
    .post<{ id: string }>(`/reservations/${id}/reviews`, {
      rating,
      comment: comment.trim() || null,
    })
    .then((r) => r.data);

export const confirmReservation = (id: string) =>
  wheelbyApi.post(`/reservations/${id}/confirm`);

export const rejectReservation = (id: string, reason: string) =>
  wheelbyApi.post(`/reservations/${id}/reject`, { id, reason });

export const startRental = (id: string) =>
  wheelbyApi.post(`/reservations/${id}/start`);

export const completeRental = (id: string) =>
  wheelbyApi.post(`/reservations/${id}/complete`);

export const cancelReservation = (id: string, reason?: string) =>
  wheelbyApi.post(`/reservations/${id}/cancel`, {
    id,
    reason: reason?.trim() || null,
  });

// ---------- Permisos ----------

export const requestPermission = (permissionCode: string) =>
  wheelbyApi
    .post<{ id: string }>("/permissions/requests", { permissionCode })
    .then((r) => r.data);

export const listMyRequests = (page: number, pageSize: number) =>
  wheelbyApi
    .get<PagedResult<PermissionRequest>>("/permissions/requests/mine", {
      params: { page, pageSize },
    })
    .then((r) => r.data);

// ---------- Documentos ----------

export const listDocuments = (page: number, pageSize: number) =>
  wheelbyApi
    .get<PagedResult<DocumentSummary>>("/documents", {
      params: { page, pageSize },
    })
    .then((r) => r.data);

export const uploadDocument = (file: File) => {
  const body = new FormData();
  body.append("file", file);
  return wheelbyApi
    .post<{ id: string }>("/documents", body)
    .then((r) => r.data);
};

// La descarga necesita el token, así que se pide como blob y se guarda desde el navegador.
export async function downloadDocument(doc: DocumentSummary) {
  const { data } = await wheelbyApi.get<Blob>(`/documents/${doc.id}/content`, {
    responseType: "blob",
  });
  saveBlob(data, doc.fileName);
}

function saveBlob(data: Blob, fileName: string) {
  const url = URL.createObjectURL(data);
  const a = document.createElement("a");
  a.href = url;
  a.download = fileName;
  a.click();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}

export const deleteDocument = (id: string) =>
  wheelbyApi.delete(`/documents/${id}`);

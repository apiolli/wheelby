import type {
  PagedResult,
  PermissionRequest,
  Reservation,
  ReservationStatus,
  Role,
  UserSummary,
} from "@/types/types";
import { wheelbyApi } from "./wheelby-api";

// Endpoints del Administrador (/admin/*). La API exige el rol en todo el grupo.

export const listUsers = (page: number, pageSize: number) =>
  wheelbyApi
    .get<PagedResult<UserSummary>>("/admin/users", {
      params: { page, pageSize },
    })
    .then((r) => r.data);

export const changeRole = (id: string, role: Role) =>
  wheelbyApi.patch(`/admin/users/${id}/role`, { role });

export const disableUser = (id: string) =>
  wheelbyApi.post(`/admin/users/${id}/disable`);

export const enableUser = (id: string) =>
  wheelbyApi.post(`/admin/users/${id}/enable`);

export const forcePasswordReset = (id: string) =>
  wheelbyApi.post(`/admin/users/${id}/force-password-reset`);

export const listPendingRequests = (page: number, pageSize: number) =>
  wheelbyApi
    .get<PagedResult<PermissionRequest>>("/admin/permissions/requests", {
      params: { page, pageSize },
    })
    .then((r) => r.data);

export const approveRequest = (id: string) =>
  wheelbyApi.post(`/admin/permissions/requests/${id}/approve`);

export const rejectRequest = (id: string, reason: string) =>
  wheelbyApi.post(`/admin/permissions/requests/${id}/reject`, { reason });

export const listAllReservations = (
  status: ReservationStatus | undefined,
  page: number,
  pageSize: number,
) =>
  wheelbyApi
    .get<PagedResult<Reservation>>("/admin/reservations", {
      params: { status, page, pageSize },
    })
    .then((r) => r.data);

import type { PermissionRequest } from "@/types/types";

// Códigos del catálogo de permisos del backend.
export const PUBLISH_VEHICLE = "wheelby.publish-vehicle";

export const PERMISSION_LABEL: Record<string, string> = {
  [PUBLISH_VEHICLE]: "Ser anfitrión (publicar vehículos)",
  "core.demo-action": "Acción de demostración",
};

export const permissionLabel = (code: string) => PERMISSION_LABEL[code] ?? code;

export type HostStatus = "none" | "pending" | "verified" | "rejected";

// El estado de anfitrión sale de la última solicitud del permiso para publicar.
export function latestHostRequest(requests: PermissionRequest[]) {
  return requests
    .filter((r) => r.permissionCode === PUBLISH_VEHICLE)
    .sort((a, b) => b.createdAt.localeCompare(a.createdAt))[0];
}

export function hostStatus(request: PermissionRequest | undefined): HostStatus {
  if (!request) return "none";
  if (request.status === "Pending") return "pending";
  if (request.status === "Rejected") return "rejected";
  return "verified";
}

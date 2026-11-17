import { Badge } from "@/components/ui/badge";
import {
  REQUEST_STATUS_LABEL,
  REQUEST_STATUS_VARIANT,
  RESERVATION_STATUS_LABEL,
  RESERVATION_STATUS_VARIANT,
  ROLE_LABEL,
  USER_STATUS_LABEL,
  USER_STATUS_VARIANT,
} from "@/lib/labels";
import type {
  PermissionRequestStatus,
  ReservationStatus,
  Role,
  UserStatus,
} from "@/types/types";

export function RoleBadge({ role }: { role: Role }) {
  return (
    <Badge variant={role === "Administrator" ? "primary" : "outline"}>
      {ROLE_LABEL[role]}
    </Badge>
  );
}

export function UserStatusBadge({ status }: { status: UserStatus }) {
  return (
    <Badge variant={USER_STATUS_VARIANT[status]}>
      {USER_STATUS_LABEL[status]}
    </Badge>
  );
}

export function ReservationStatusBadge({
  status,
  className,
}: {
  status: ReservationStatus;
  className?: string;
}) {
  return (
    <Badge variant={RESERVATION_STATUS_VARIANT[status]} className={className}>
      {RESERVATION_STATUS_LABEL[status]}
    </Badge>
  );
}

export function RequestStatusBadge({
  status,
}: {
  status: PermissionRequestStatus;
}) {
  return (
    <Badge variant={REQUEST_STATUS_VARIANT[status]}>
      {REQUEST_STATUS_LABEL[status]}
    </Badge>
  );
}

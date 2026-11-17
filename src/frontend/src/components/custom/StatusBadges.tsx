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
import {
  ReservationStatus,
  type PermissionRequestStatus,
  type Role,
  type UserStatus,
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
  const live = status === ReservationStatus.InProgress;
  return (
    <Badge variant={RESERVATION_STATUS_VARIANT[status]} className={className}>
      {live && (
        <span aria-hidden="true" className="relative mr-0.5 flex size-1.5">
          <span className="absolute inset-0 animate-ping rounded-full bg-primary opacity-60 motion-reduce:hidden" />
          <span className="relative size-1.5 rounded-full bg-primary" />
        </span>
      )}
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

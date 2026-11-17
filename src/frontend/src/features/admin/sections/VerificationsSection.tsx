import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AnimatePresence, m } from "motion/react";
import { CircleAlert, Inbox, KeyRound } from "lucide-react";

import {
  approveRequest,
  listPendingRequests,
  rejectRequest,
} from "@/api/admin.api";
import { errorMessage } from "@/api/errors";
import ConfirmAction from "@/components/custom/ConfirmAction";
import DisabledHint from "@/components/custom/DisabledHint";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import ReasonAction from "@/features/profile/components/ReasonAction";
import { formatDate, initial, shortId } from "@/lib/format";
import { permissionLabel, PUBLISH_VEHICLE } from "@/lib/permissions";
import { queryKeys } from "@/lib/query-keys";
import { showToast, useSession } from "@/stores/session.store";
import type { PermissionRequest } from "@/types/types";
import AdminHeading from "../components/AdminHeading";
import { useUserLookup } from "../hooks/useAllUsers";

// Solicitudes de permiso pendientes (la más relevante: ser anfitrión). La API solo lista las
// pendientes; al aprobar o rechazar, la tarjeta sale y las demás suben (layout de Motion).
export default function VerificationsSection() {
  const queryClient = useQueryClient();
  const me = useSession((s) => s.user);
  const lookup = useUserLookup();
  const requests = useQuery({
    queryKey: queryKeys.adminRequests,
    queryFn: () => listPendingRequests(1, 100),
  });

  const resolve = useMutation({
    mutationFn: ({ run }: { run: () => Promise<unknown>; done: string }) =>
      run(),
    onSuccess: (_, { done }) => {
      showToast(done);
      return queryClient.invalidateQueries({
        queryKey: queryKeys.adminRequests,
      });
    },
  });

  // Las pendientes, de la más antigua a la más reciente.
  const items = [...(requests.data?.items ?? [])].sort((a, b) =>
    a.createdAt.localeCompare(b.createdAt),
  );

  return (
    <>
      <AdminHeading
        title="Verificaciones"
        description="Solicitudes de permiso pendientes, como la de quienes quieren ser anfitriones."
      >
        {requests.data && (
          <span className="rounded-full bg-background px-3 py-1 text-[13px] font-semibold tabular-nums">
            {requests.data.totalItems} pendientes
          </span>
        )}
      </AdminHeading>

      {requests.isPending ? (
        <ul className="grid gap-4 xl:grid-cols-2" aria-busy="true">
          {[0, 1].map((i) => (
            <li
              key={i}
              className="h-48 animate-pulse rounded-2xl bg-background"
            />
          ))}
        </ul>
      ) : requests.isError ? (
        <Alert variant="destructive">
          <CircleAlert />
          <AlertDescription>{errorMessage(requests.error)}</AlertDescription>
        </Alert>
      ) : items.length === 0 ? (
        <div className="flex flex-col items-center rounded-2xl border border-dashed border-border bg-background px-6 py-12 text-center">
          <Inbox className="size-6 text-muted-foreground" strokeWidth={1.6} />
          <p className="mt-3 max-w-85 text-[14px] text-muted-foreground">
            No hay solicitudes pendientes. Cuando alguien pida ser anfitrión,
            aparecerá aquí.
          </p>
        </div>
      ) : (
        <ul className="grid gap-4 xl:grid-cols-2">
          <AnimatePresence initial={false}>
            {items.map((r) => (
              <RequestCard
                key={r.id}
                request={r}
                requester={lookup(r.requesterId)}
                isOwn={r.requesterId === me?.id}
                onApprove={(name) =>
                  resolve.mutateAsync({
                    run: () => approveRequest(r.id),
                    done:
                      r.permissionCode === PUBLISH_VEHICLE
                        ? `${name} ya es anfitrión verificado`
                        : "Solicitud aprobada",
                  })
                }
                onReject={(reason) =>
                  resolve.mutateAsync({
                    run: () => rejectRequest(r.id, reason),
                    done: "Solicitud rechazada",
                  })
                }
              />
            ))}
          </AnimatePresence>
        </ul>
      )}
    </>
  );
}

interface RequestCardProps {
  request: PermissionRequest;
  requester: { fullName: string; email: string } | undefined;
  isOwn: boolean;
  onApprove: (name: string) => Promise<unknown>;
  onReject: (reason: string) => Promise<unknown>;
}

function RequestCard({
  request,
  requester,
  isOwn,
  onApprove,
  onReject,
}: RequestCardProps) {
  const name = requester?.fullName ?? `Cuenta #${shortId(request.requesterId)}`;
  const approve = <Button disabled={isOwn}>Aprobar</Button>;
  const reject = (
    <Button variant="destructive-outline" disabled={isOwn}>
      Rechazar
    </Button>
  );

  return (
    <m.li
      layout
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{
        opacity: 0,
        scale: 0.94,
        transition: { duration: 0.3, delay: 0.2 },
      }}
      aria-label={`Solicitud de ${name}`}
      className="flex flex-col rounded-2xl border border-border-soft bg-background p-5"
    >
      <div className="flex items-start gap-3">
        <span className="grid size-10 shrink-0 place-items-center rounded-full bg-primary-soft font-bold text-primary">
          {initial(name)}
        </span>
        <div className="min-w-0 flex-1">
          <p className="truncate font-semibold">{name}</p>
          {requester && (
            <p className="truncate text-[13px] text-muted-foreground">
              {requester.email}
            </p>
          )}
        </div>
        <p className="shrink-0 text-right text-[12.5px] text-muted-foreground">
          Solicitó el
          <br />
          <span className="font-semibold text-foreground">
            {formatDate(request.createdAt)}
          </span>
        </p>
      </div>

      <p className="mt-5 flex items-center gap-2 rounded-xl bg-secondary p-3 text-[13.5px]">
        <KeyRound
          className="size-4 shrink-0 text-muted-foreground"
          strokeWidth={1.8}
        />
        <span>
          Pide:{" "}
          <strong className="font-semibold">
            {permissionLabel(request.permissionCode)}
          </strong>
        </span>
      </p>

      <div className="mt-auto pt-5">
        <div className="flex flex-wrap gap-2 border-t border-border-soft pt-4">
          {isOwn ? (
            <>
              <DisabledHint hint="No puedes resolver tu propia solicitud.">
                {approve}
              </DisabledHint>
              <DisabledHint hint="No puedes resolver tu propia solicitud.">
                {reject}
              </DisabledHint>
            </>
          ) : (
            <>
              <ConfirmAction
                trigger={approve}
                title={`¿Aprobar la solicitud de ${name}?`}
                description={
                  request.permissionCode === PUBLISH_VEHICLE
                    ? "Podrá publicar vehículos en el catálogo."
                    : `Obtendrá el permiso «${permissionLabel(request.permissionCode)}».`
                }
                confirmLabel="Aprobar"
                onConfirm={() => onApprove(name)}
              />
              <ReasonAction
                id={`reason-${request.id}`}
                trigger={reject}
                title={`¿Rechazar la solicitud de ${name}?`}
                description="Le mostraremos el motivo en su perfil para que pueda corregirlo y volver a solicitarlo."
                confirmLabel="Rechazar solicitud"
                note="Escribe al menos 10 caracteres. El solicitante lo verá tal cual."
                onConfirm={onReject}
              />
            </>
          )}
        </div>
      </div>
    </m.li>
  );
}

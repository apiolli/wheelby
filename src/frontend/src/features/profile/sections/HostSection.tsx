import type { ReactNode } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { m } from "motion/react";
import {
  BadgeCheck,
  CircleAlert,
  Clock,
  KeyRound,
  LoaderCircle,
  type LucideIcon,
} from "lucide-react";
import { Link } from "react-router";

import { errorMessage } from "@/api/errors";
import { listMyRequests, requestPermission } from "@/api/wheelby.api";
import SectionHeading from "@/components/custom/SectionHeading";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button, buttonVariants } from "@/components/ui/button";
import { formatDate } from "@/lib/format";
import { ENTER } from "@/lib/motion";
import {
  hostStatus,
  latestHostRequest,
  PUBLISH_VEHICLE,
} from "@/lib/permissions";
import { queryKeys } from "@/lib/query-keys";
import { showToast } from "@/stores/session.store";
import PublishVehicleForm from "../components/PublishVehicleForm";

// Ser anfitrión = tener el permiso wheelby.publish-vehicle, que concede un Administrador.
export default function HostSection() {
  const queryClient = useQueryClient();
  const requests = useQuery({
    queryKey: queryKeys.myRequests,
    queryFn: () => listMyRequests(1, 100),
  });

  const apply = useMutation({
    mutationFn: () => requestPermission(PUBLISH_VEHICLE),
    onSuccess: () => {
      showToast("Enviamos tu solicitud. Te avisaremos cuando la revisemos");
      return queryClient.invalidateQueries({ queryKey: queryKeys.myRequests });
    },
    onError: (err) => showToast(errorMessage(err)),
  });

  const request = requests.data
    ? latestHostRequest(requests.data.items)
    : undefined;
  const status = hostStatus(request);

  const applyButton = (label: string, variant: "outline" | "default") => (
    <Button
      variant={variant}
      size="lg"
      onClick={() => apply.mutate()}
      disabled={apply.isPending}
      aria-busy={apply.isPending}
    >
      {apply.isPending && <LoaderCircle className="animate-spin" />}
      {apply.isPending ? "Enviando…" : label}
    </Button>
  );

  return (
    <>
      <SectionHeading
        title="Anfitrión"
        description="Comparte tu vehículo cuando no lo uses y genera ingresos."
      />

      {requests.isPending ? (
        <div
          className="h-48 animate-pulse rounded-2xl bg-highlight-soft"
          aria-busy="true"
        />
      ) : requests.isError ? (
        <Alert variant="destructive">
          <CircleAlert />
          <AlertDescription>{errorMessage(requests.error)}</AlertDescription>
        </Alert>
      ) : status === "none" ? (
        <HostCard
          icon={KeyRound}
          title="Conviértete en anfitrión"
          // Es una invitación: botón con borde, nunca primario relleno.
          action={applyButton("Conviértete en anfitrión", "outline")}
        >
          <p>
            Publica tu vehículo y decide cuándo alquilarlo. Nuestro equipo
            revisa cada solicitud antes de que puedas publicar.
          </p>
        </HostCard>
      ) : status === "pending" ? (
        <HostCard icon={Clock} title="Estamos revisando tu solicitud">
          <p>
            La enviaste el {formatDate(request!.createdAt)}. Te avisaremos
            cuando un administrador la revise.
          </p>
        </HostCard>
      ) : status === "verified" ? (
        <>
          <HostCard
            icon={BadgeCheck}
            title="Ya eres anfitrión"
            badge={
              <Badge variant="default">
                <BadgeCheck
                  className="text-highlight-strong"
                  strokeWidth={2.2}
                />
                Anfitrión verificado
              </Badge>
            }
            action={
              <Link
                to="/perfil/solicitudes"
                className={buttonVariants({ variant: "outline", size: "lg" })}
              >
                Ver solicitudes de reserva
              </Link>
            }
          >
            <p>
              Aprobamos tu solicitud
              {request?.resolvedAt
                ? ` el ${formatDate(request.resolvedAt)}`
                : ""}
              . Ya puedes publicar vehículos.
            </p>
          </HostCard>
          <PublishVehicleForm />
        </>
      ) : (
        <HostCard
          icon={CircleAlert}
          title="No pudimos aprobar tu solicitud"
          action={applyButton("Volver a solicitar", "default")}
        >
          <p>Revisa el motivo y vuelve a enviarla cuando lo hayas resuelto.</p>
          {request?.rejectionReason && (
            <div className="mt-4 rounded-xl bg-background p-4">
              <p className="text-[12.5px] font-bold text-foreground">Motivo</p>
              <p className="mt-0.5 text-foreground">
                {request.rejectionReason}
              </p>
            </div>
          )}
        </HostCard>
      )}
    </>
  );
}

interface HostCardProps {
  icon: LucideIcon;
  title: string;
  badge?: ReactNode;
  action?: ReactNode;
  children: ReactNode;
}

// Tarjeta destacada sobre el ámbar suave de la marca; los botones siguen siendo petróleo.
function HostCard({
  icon: Icon,
  title,
  badge,
  action,
  children,
}: HostCardProps) {
  return (
    <m.section
      key={title}
      {...ENTER}
      role="status"
      className="rounded-2xl bg-highlight-soft p-6 md:p-8"
    >
      <div className="flex flex-col gap-5 sm:flex-row sm:items-start">
        <span className="grid size-12 shrink-0 place-items-center rounded-full bg-background text-highlight-strong">
          <Icon className="size-6" strokeWidth={1.6} />
        </span>
        <div className="min-w-0 flex-1">
          {badge && <div className="mb-2">{badge}</div>}
          <h2 className="text-xl leading-tight font-bold tracking-tight">
            {title}
          </h2>
          <div className="mt-2 max-w-130 text-[14.5px] text-highlight-foreground">
            {children}
          </div>
          {action && <div className="mt-5">{action}</div>}
        </div>
      </div>
    </m.section>
  );
}

import type { ReactNode } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
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
import { requestPermission } from "@/api/wheelby.api";
import SectionHeading from "@/components/custom/SectionHeading";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button, buttonVariants } from "@/components/ui/button";
import { useHostStatus } from "@/features/host/useHostStatus";
import { features } from "@/lib/features";
import { formatDate } from "@/lib/format";
import { ENTER } from "@/lib/motion";
import { PUBLISH_VEHICLE } from "@/lib/permissions";
import { queryKeys } from "@/lib/query-keys";
import { DOCUMENT_FORMATS_LABEL } from "@/lib/schemas";
import { showToast } from "@/stores/session.store";
import DocumentsManager from "../components/DocumentsManager";

// Ser anfitrión = tener el permiso wheelby.publish-vehicle, que concede un Administrador. La
// tarjeta tiene cuatro variantes según la última solicitud; debajo, los documentos propios.
export default function HostSection() {
  const queryClient = useQueryClient();
  const requests = useHostStatus();

  const apply = useMutation({
    mutationFn: () => requestPermission(PUBLISH_VEHICLE),
    onSuccess: () => {
      showToast("Enviamos tu solicitud. Te avisaremos cuando la revisemos");
      return queryClient.invalidateQueries({ queryKey: queryKeys.myRequests });
    },
    onError: (err) => showToast(errorMessage(err)),
  });

  const { request, status } = requests;

  const applyButton = (label: string, variant: "outline" | "default") => (
    <Button
      variant={variant}
      size="lg"
      className="rounded-full"
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
          <p>Publica tus vehículos y empieza a generar ingresos.</p>
        </HostCard>
      ) : status === "pending" ? (
        <HostCard icon={Clock} title="Tu solicitud está en revisión.">
          <p>
            La enviaste el {formatDate(request!.createdAt)}. Te avisaremos
            cuando un administrador la revise.
          </p>
        </HostCard>
      ) : status === "verified" ? (
        <HostCard
          icon={BadgeCheck}
          title="Eres anfitrión verificado."
          badge={
            <Badge variant="default">
              <BadgeCheck className="text-highlight-strong" strokeWidth={2.2} />
              Anfitrión verificado
            </Badge>
          }
          action={
            <Link
              to={
                features.myVehicles
                  ? "/anfitrion/vehiculos"
                  : "/anfitrion/vehiculos/nuevo"
              }
              className={buttonVariants({
                size: "lg",
                className: "rounded-full",
              })}
            >
              Ir a Mis vehículos
            </Link>
          }
        >
          {/* La fecha solo si la solicitud la trae. */}
          {request?.resolvedAt && (
            <p>Verificado desde el {formatDate(request.resolvedAt)}.</p>
          )}
          <p>Publica vehículos y gestiona las reservas que recibes.</p>
        </HostCard>
      ) : (
        <HostCard
          icon={CircleAlert}
          title="No pudimos aprobar tu solicitud"
          action={applyButton("Volver a solicitar", "default")}
        >
          <p>
            Un administrador rechazó tu solicitud. Revisa el motivo y vuelve a
            enviarla cuando lo hayas resuelto.
          </p>
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

      <section aria-labelledby="my-docs" className="mt-10">
        <DocumentsManager
          restrictTypes
          heading={(upload) => (
            <div className="mb-4 flex flex-wrap items-end justify-between gap-3">
              <div>
                <h2 id="my-docs" className="text-lg font-bold tracking-tight">
                  Mis documentos
                </h2>
                <p className="text-[14px] text-muted-foreground">
                  Licencia de conducir, matrícula del vehículo… ·{" "}
                  {DOCUMENT_FORMATS_LABEL}
                </p>
              </div>
              {upload}
            </div>
          )}
          footnote="Solo tú y el equipo de Wheelby pueden ver estos documentos."
        />
      </section>
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

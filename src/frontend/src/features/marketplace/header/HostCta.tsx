import { Clock, KeyRound, LayoutDashboard } from "lucide-react";
import { Link } from "react-router";
import { cn } from "cn";

import { buttonVariants } from "@/components/ui/button";
import { useHostStatus } from "@/features/host/useHostStatus";

// Acción destacada del header según quién mira:
// - Estándar sin solicitud o rechazada: "Conviértete en anfitrión".
// - Con solicitud pendiente: chip informativo, sin acción.
// - Anfitrión verificado: "Modo anfitrión" (Mis vehículos).
export function HostCta({ className }: { className?: string }) {
  const { status, isPending } = useHostStatus();
  if (isPending) return null;

  if (status === "pending")
    return (
      <span
        className={cn(
          "inline-flex h-9 items-center gap-1.5 rounded-full bg-highlight-soft px-3.5 text-[13px] font-semibold text-highlight-foreground",
          className,
        )}
      >
        <Clock className="size-3.5" strokeWidth={2} />
        Solicitud en revisión
      </span>
    );

  if (status === "verified")
    return (
      <Link
        to="/anfitrion/vehiculos"
        className={cn(
          buttonVariants({ variant: "ghost" }),
          "h-11 gap-2 px-3.5 text-sm",
          className,
        )}
      >
        <KeyRound className="size-4" strokeWidth={1.8} />
        Modo anfitrión
      </Link>
    );

  // Es una invitación, no la acción principal: botón ghost.
  return (
    <Link
      to="/perfil/anfitrion"
      className={cn(
        buttonVariants({ variant: "ghost" }),
        "h-11 px-3.5 text-sm",
        className,
      )}
    >
      Conviértete en anfitrión
    </Link>
  );
}

// Administrador: chip con borde teal hacia su panel, en lugar del CTA de anfitrión.
export function AdminPanelChip({ className }: { className?: string }) {
  return (
    <Link
      to="/admin"
      className={cn(
        "inline-flex h-9 items-center gap-1.5 rounded-full border border-primary px-3.5 text-[13px] font-semibold text-primary outline-none hover:bg-primary-soft focus-visible:ring-2 focus-visible:ring-ring",
        className,
      )}
    >
      <LayoutDashboard className="size-3.5" strokeWidth={2} />
      Panel de administración
    </Link>
  );
}

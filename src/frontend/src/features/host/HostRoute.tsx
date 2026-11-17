import { BadgeCheck, CircleAlert } from "lucide-react";
import { Link, Outlet } from "react-router";

import { errorMessage } from "@/api/errors";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { buttonVariants } from "@/components/ui/button";
import { useHostStatus } from "./useHostStatus";

// Las rutas /anfitrion/* exigen el permiso de publicar. Sin él, la variante 403
// "Necesitas ser anfitrión verificado" (su diseño final va aparte; esta es la provisional).
export default function HostRoute() {
  const host = useHostStatus();

  if (host.isPending)
    return (
      <main
        className="min-h-[60vh]"
        aria-busy="true"
        aria-label="Comprobando tu permiso de anfitrión"
      />
    );

  if (host.isError)
    return (
      <main className="mx-auto max-w-160 px-gutter py-12">
        <Alert variant="destructive">
          <CircleAlert />
          <AlertDescription>{errorMessage(host.error)}</AlertDescription>
        </Alert>
      </main>
    );

  if (host.status !== "verified")
    return (
      <main className="mx-auto grid max-w-120 justify-items-center gap-4 px-gutter py-16 text-center">
        <span className="grid size-14 place-items-center rounded-full bg-highlight-soft text-highlight-strong">
          <BadgeCheck className="size-7" strokeWidth={1.6} />
        </span>
        <p className="text-[13px] font-bold tracking-wide text-muted-foreground">
          403
        </p>
        <h1 className="text-2xl font-bold tracking-tight">
          Necesitas ser anfitrión verificado
        </h1>
        <p className="text-[14.5px] text-muted-foreground">
          {host.status === "pending"
            ? "Tu solicitud está en revisión. Te avisaremos cuando un administrador la apruebe."
            : "Para publicar y gestionar vehículos, solicita el permiso de anfitrión desde tu perfil."}
        </p>
        <Link
          to="/perfil/anfitrion"
          className={buttonVariants({ size: "lg", className: "rounded-full" })}
        >
          Ir a Perfil · Anfitrión
        </Link>
      </main>
    );

  return <Outlet />;
}

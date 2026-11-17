import { Outlet } from "react-router";

import { PageError } from "@/components/custom/ErrorStates";
import { ForbiddenPage } from "@/features/errors/ErrorPages";
import { useHostStatus } from "./useHostStatus";

// Las rutas /anfitrion/* exigen el permiso de publicar. Sin él, la variante 403
// "Necesitas ser anfitrión verificado".
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
    return <PageError error={host.error} onRetry={() => host.refetch()} />;

  if (host.status !== "verified") return <ForbiddenPage variant="host" />;

  return <Outlet />;
}

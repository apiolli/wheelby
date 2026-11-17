import { Navigate, Outlet, useLocation } from "react-router";

import Splash from "@/components/custom/Splash";
import { loginPath } from "@/lib/redirect";
import { isAdmin, useSession } from "@/stores/session.store";
import type { AuthRouteState } from "@/types/types";

interface ProtectedRouteProps {
  // Solo para Administradores (si no, vuelve a /).
  requireAdmin?: boolean;
  // Solo para el rol Estándar: el Administrador no alquila ni publica (la API se lo niega).
  requireStandard?: boolean;
}

// Todo lo que no es una página de acceso cuelga de aquí. Sin sesión, al login con ?redirect=;
// con token, nada se muestra hasta que GET /auth/me lo confirma.
export default function ProtectedRoute({
  requireAdmin,
  requireStandard,
}: ProtectedRouteProps) {
  const status = useSession((s) => s.status);
  const endReason = useSession((s) => s.endReason);
  const user = useSession((s) => s.user);
  const location = useLocation();

  if (status === "checking") return <Splash />;

  if (status === "anonymous" || !user) {
    const from = location.pathname + location.search;
    const state: AuthRouteState | undefined =
      endReason === "expired" ? { notice: "expired" } : undefined;
    return (
      <Navigate
        to={endReason === "logout" ? "/auth/login" : loginPath(from)}
        state={state}
        replace
      />
    );
  }

  if (requireAdmin && !isAdmin(user)) return <Navigate to="/" replace />;
  if (requireStandard && isAdmin(user)) return <Navigate to="/admin" replace />;

  return <Outlet />;
}

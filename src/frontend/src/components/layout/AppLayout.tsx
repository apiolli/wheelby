import { Suspense, type ReactNode } from "react";
import { Outlet } from "react-router";

import SiteHeader from "@/features/marketplace/header/SiteHeader";
import Footer from "@/features/marketplace/catalog/components/Footer";
import { isAdmin, useSession } from "@/stores/session.store";
import MobileTabBar from "./MobileTabBar";

// Layout del marketplace con sesión: header, contenido de la ruta y footer. Solo se carga
// después de que ProtectedRoute confirma la sesión.
// children: contenido fijo en vez de la ruta (p. ej. el 403 de una ruta de Administrador).
export default function AppLayout({ children }: { children?: ReactNode }) {
  const user = useSession((s) => s.user);
  if (!user) return null;

  return (
    <>
      <SiteHeader user={user} />
      {/* Reserva altura mientras carga una página diferida, para que el footer no salte. */}
      <Suspense fallback={<div className="min-h-[60vh]" />}>
        {children ?? <Outlet />}
      </Suspense>
      <Footer />
      {/* Móvil: barra inferior (el Administrador entra a su panel desde Perfil). */}
      {!isAdmin(user) && (
        <>
          <MobileTabBar />
          <div
            aria-hidden="true"
            className="h-[calc(3.5rem+env(safe-area-inset-bottom))] md:hidden"
          />
        </>
      )}
    </>
  );
}

import { Suspense } from "react";
import { Outlet } from "react-router";

import SiteHeader from "@/features/marketplace/header/SiteHeader";
import Footer from "@/features/marketplace/catalog/components/Footer";
import { useSession } from "@/stores/session.store";

// Layout del marketplace con sesión: header, contenido de la ruta y footer. Solo se carga
// después de que ProtectedRoute confirma la sesión.
export default function AppLayout() {
  const user = useSession((s) => s.user);
  if (!user) return null;

  return (
    <>
      <SiteHeader user={user} />
      {/* Reserva altura mientras carga una página diferida, para que el footer no salte. */}
      <Suspense fallback={<div className="min-h-[60vh]" />}>
        <Outlet />
      </Suspense>
      <Footer />
    </>
  );
}

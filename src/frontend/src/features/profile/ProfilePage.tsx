import { ChevronLeft, ChevronRight } from "lucide-react";
import { m } from "motion/react";
import {
  Link,
  Navigate,
  NavLink,
  Route,
  Routes,
  useLocation,
} from "react-router";
import { cn } from "cn";

import { useMediaQuery } from "@/hooks/useMediaQuery";
import { initial } from "@/lib/format";
import { ENTER } from "@/lib/motion";
import { isAdmin, useSession } from "@/stores/session.store";
import { PROFILE_SECTIONS } from "./sections";
import AccountSection from "./sections/AccountSection";
import DocumentsSection from "./sections/DocumentsSection";
import HostSection from "./sections/HostSection";
import ReservationsSection from "./sections/ReservationsSection";
import SecuritySection from "./sections/SecuritySection";
import HostRequestsSection from "./sections/HostRequestsSection";

// Perfil: sidebar de secciones + contenido en escritorio; en móvil, la lista de secciones es la
// portada (/perfil) y cada sección se abre en su propia ruta con "volver".
export default function ProfilePage() {
  const { pathname } = useLocation();
  const user = useSession((s) => s.user);
  const admin = isAdmin(user);
  const sections = PROFILE_SECTIONS.filter((s) => !admin || !s.standardOnly);
  const isIndex = pathname.replace(/\/$/, "") === "/perfil";

  return (
    <main className="px-gutter pt-4 pb-16 md:pt-8">
      <div className="mx-auto max-w-280 md:grid md:grid-cols-[232px_minmax(0,1fr)] md:gap-12 lg:gap-16">
        <aside className="hidden md:block">
          <div className="sticky top-28">
            <ProfileIdentity />
            <nav aria-label="Secciones del perfil" className="mt-6 grid gap-1">
              {sections.map(({ id, label, icon: Icon }) => (
                <NavLink
                  key={id}
                  to={`/perfil/${id}`}
                  className={({ isActive }) =>
                    cn(
                      "flex h-11 items-center gap-3 rounded-full px-4 text-[14.5px] transition-colors outline-none focus-visible:ring-2 focus-visible:ring-ring",
                      isActive
                        ? "bg-secondary font-semibold text-foreground"
                        : "text-muted-foreground hover:bg-secondary hover:text-foreground",
                    )
                  }
                >
                  <Icon className="size-4.5" strokeWidth={1.7} />
                  {label}
                </NavLink>
              ))}
            </nav>
          </div>
        </aside>

        <div className="min-w-0">
          {!isIndex && (
            <Link
              to="/perfil"
              className="mb-4 -ml-1 inline-flex items-center gap-1 rounded-full py-1 pr-2 text-sm font-semibold outline-none focus-visible:ring-2 focus-visible:ring-ring md:hidden"
            >
              <ChevronLeft className="size-4" strokeWidth={2.2} />
              Perfil
            </Link>
          )}
          {/* Cada sección entra con un fade corto al navegar. */}
          <m.div key={pathname} {...ENTER}>
            <Routes>
              <Route index element={<ProfileIndex />} />
              <Route path="cuenta" element={<AccountSection />} />
              <Route path="seguridad" element={<SecuritySection />} />
              <Route path="documentos" element={<DocumentsSection />} />
              {!admin && (
                <>
                  <Route path="reservas" element={<ReservationsSection />} />
                  <Route path="anfitrion" element={<HostSection />} />
                  <Route path="solicitudes" element={<HostRequestsSection />} />
                </>
              )}
              <Route path="*" element={<Navigate to="/perfil" replace />} />
            </Routes>
          </m.div>
        </div>
      </div>
    </main>
  );
}

function ProfileIdentity() {
  const user = useSession((s) => s.user);
  if (!user) return null;
  return (
    <div className="flex items-center gap-3">
      <span className="grid size-12 shrink-0 place-items-center rounded-full bg-primary text-lg font-bold text-primary-foreground">
        {initial(user.fullName)}
      </span>
      <div className="min-w-0">
        <p className="truncate font-bold">{user.fullName}</p>
        <p className="truncate text-[13px] text-muted-foreground">
          {user.email}
        </p>
      </div>
    </div>
  );
}

// En escritorio la portada es "Cuenta"; en móvil, la lista de secciones.
function ProfileIndex() {
  const isDesktop = useMediaQuery("(min-width: 768px)");
  const user = useSession((s) => s.user);
  const sections = PROFILE_SECTIONS.filter(
    (s) => !isAdmin(user) || !s.standardOnly,
  );
  if (isDesktop) return <Navigate to="/perfil/cuenta" replace />;

  return (
    <>
      <h1 className="mb-5 text-[26px] leading-tight font-bold tracking-tight">
        Perfil
      </h1>
      <ProfileIdentity />
      <nav aria-label="Secciones del perfil" className="mt-6">
        <ul className="divide-y divide-border-soft border-y border-border-soft">
          {sections.map(({ id, label, description, icon: Icon }) => (
            <li key={id}>
              <Link
                to={`/perfil/${id}`}
                className="flex items-center gap-4 py-4 outline-none focus-visible:bg-secondary"
              >
                <Icon className="size-6 shrink-0" strokeWidth={1.5} />
                <span className="min-w-0 flex-1">
                  <span className="block text-[15px] font-semibold">
                    {label}
                  </span>
                  <span className="block truncate text-[13px] text-muted-foreground">
                    {description}
                  </span>
                </span>
                <ChevronRight
                  className="size-5 text-muted-foreground"
                  strokeWidth={1.8}
                />
              </Link>
            </li>
          ))}
        </ul>
      </nav>
    </>
  );
}

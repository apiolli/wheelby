import { useState } from "react";
import { m } from "motion/react";
import { ArrowUpRight, LogOut, Menu } from "lucide-react";
import {
  Link,
  Navigate,
  NavLink,
  Route,
  Routes,
  useLocation,
} from "react-router";
import { cn } from "cn";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Sheet, SheetContent, SheetTitle } from "@/components/ui/sheet";
import Logo from "@/features/marketplace/header/Logo";
import DocumentsSection from "@/features/profile/sections/DocumentsSection";
import { initial } from "@/lib/format";
import { ENTER } from "@/lib/motion";
import { useSession } from "@/stores/session.store";
import { ADMIN_NAV } from "./nav";
import OverviewSection from "./sections/OverviewSection";
import ReservationsSection from "./sections/ReservationsSection";
import UsersSection from "./sections/UsersSection";
import VerificationsSection from "./sections/VerificationsSection";

// Panel de administración: layout de consola de trabajo, sin el header ni el footer del
// marketplace. Sidebar fija en escritorio; en móvil, barra superior con el menú en un Sheet.
export default function AdminPage() {
  const [menuOpen, setMenuOpen] = useState(false);
  const { pathname } = useLocation();

  return (
    <div className="min-h-dvh bg-secondary">
      <aside className="fixed inset-y-0 left-0 z-30 hidden w-72 flex-col border-r border-border-soft bg-background md:flex">
        <SidebarContent />
      </aside>

      <header className="sticky top-0 z-30 flex h-14 items-center gap-2 border-b border-border-soft bg-background px-3 md:hidden">
        <Button
          variant="ghost"
          size="icon"
          aria-label="Abrir menú"
          onClick={() => setMenuOpen(true)}
        >
          <Menu className="size-5" />
        </Button>
        <Logo variant="symbol" to="/admin" className="[&_img]:size-8" />
        <span className="text-[15px] font-bold">Administración</span>
      </header>
      <Sheet open={menuOpen} onOpenChange={setMenuOpen}>
        <SheetContent side="left">
          <SheetTitle className="sr-only">Menú de administración</SheetTitle>
          <SidebarContent onNavigate={() => setMenuOpen(false)} />
        </SheetContent>
      </Sheet>

      <main className="px-4 py-6 md:ml-72 md:px-8 md:py-8 lg:px-10">
        <m.div key={pathname} {...ENTER} className="mx-auto max-w-300">
          <Routes>
            <Route index element={<OverviewSection />} />
            <Route path="usuarios" element={<UsersSection />} />
            <Route path="verificaciones" element={<VerificationsSection />} />
            <Route path="reservas" element={<ReservationsSection />} />
            <Route
              path="documentos"
              element={
                <div className="rounded-2xl border border-border-soft bg-background p-5 md:p-6">
                  <DocumentsSection />
                </div>
              }
            />
            <Route path="*" element={<Navigate to="/admin" replace />} />
          </Routes>
        </m.div>
      </main>
    </div>
  );
}

function SidebarContent({ onNavigate }: { onNavigate?: () => void }) {
  const user = useSession((s) => s.user);
  const logout = useSession((s) => s.logout);

  return (
    <div className="flex h-full flex-col">
      <div className="flex h-16 items-center gap-2.5 px-5">
        <Logo variant="full" to="/admin" className="[&_img]:h-8" />
        <Badge variant="soft">Admin</Badge>
      </div>

      <nav
        aria-label="Panel de administración"
        className="grid gap-0.5 px-3 pt-4"
      >
        {ADMIN_NAV.map(({ to, label, icon: Icon, end }) => (
          <NavLink
            key={to}
            to={to}
            end={end}
            onClick={onNavigate}
            className={({ isActive }) =>
              cn(
                "flex h-10 items-center gap-3 rounded-lg px-3 text-[14px] transition-colors outline-none focus-visible:ring-2 focus-visible:ring-ring",
                isActive
                  ? "bg-primary-soft font-semibold text-primary"
                  : "text-muted-foreground hover:bg-secondary hover:text-foreground",
              )
            }
          >
            <Icon className="size-4.5 shrink-0" strokeWidth={1.8} />
            {label}
          </NavLink>
        ))}
      </nav>

      <div className="mt-auto border-t border-border-soft p-3">
        {user && (
          <div className="flex items-center gap-3 px-2 py-2">
            <span className="grid size-9 shrink-0 place-items-center rounded-full bg-primary text-sm font-bold text-primary-foreground">
              {initial(user.fullName)}
            </span>
            <div className="min-w-0 text-[13px] leading-tight">
              <p className="truncate font-semibold">{user.fullName}</p>
              <p className="truncate text-muted-foreground">{user.email}</p>
            </div>
          </div>
        )}
        <div className="mt-1 grid gap-0.5">
          <Link
            to="/perfil"
            onClick={onNavigate}
            className="flex h-9 items-center gap-3 rounded-lg px-3 text-[13.5px] text-muted-foreground transition-colors outline-none hover:bg-secondary hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring"
          >
            <ArrowUpRight className="size-4" strokeWidth={1.8} />
            Mi perfil
          </Link>
          <button
            type="button"
            onClick={logout}
            className="flex h-9 items-center gap-3 rounded-lg px-3 text-left text-[13.5px] text-muted-foreground transition-colors outline-none hover:bg-secondary hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring"
          >
            <LogOut className="size-4" strokeWidth={1.8} />
            Cerrar sesión
          </button>
        </div>
      </div>
    </div>
  );
}

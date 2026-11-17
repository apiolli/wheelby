import { m, useReducedMotion } from "motion/react";
import { Link, useLocation } from "react-router";
import { cn } from "cn";

import { useHostStatus } from "@/features/host/useHostStatus";
import { mobileTabs } from "@/features/marketplace/header/nav";
import { useScrollHidden } from "@/hooks/useScrollHidden";
import { HEADER_SPRING } from "@/lib/motion";

// Rutas con su propia barra inferior (precio y "Reservar"): ahí no va la de pestañas.
const OWN_BOTTOM_BAR = [/^\/vehiculos\//, /^\/reservar\//];

// Barra inferior en móvil: Explorar, Reservas y Perfil (+ Anfitrión si está verificado).
// Activa en teal, áreas táctiles de 56 px, respeta el área segura de iOS y se oculta al bajar.
export default function MobileTabBar() {
  const { pathname } = useLocation();
  const { status } = useHostStatus();
  const hidden = useScrollHidden();
  const reduce = useReducedMotion();

  if (OWN_BOTTOM_BAR.some((r) => r.test(pathname))) return null;

  return (
    <m.nav
      aria-label="Navegación principal"
      initial={false}
      animate={{ y: hidden ? "100%" : 0 }}
      transition={reduce ? { duration: 0 } : HEADER_SPRING}
      className="fixed inset-x-0 bottom-0 z-40 border-t border-border-soft bg-background/95 pb-safe backdrop-blur md:hidden"
    >
      <ul className="flex">
        {mobileTabs(status).map(({ to, label, icon: Icon, match }) => {
          const active = match(pathname);
          return (
            <li key={to} className="flex-1">
              <Link
                to={to}
                aria-current={active ? "page" : undefined}
                className={cn(
                  "flex h-14 flex-col items-center justify-center gap-1 text-[11.5px] font-semibold outline-none focus-visible:bg-secondary",
                  active ? "text-primary" : "text-muted-foreground",
                )}
              >
                <Icon className="size-6" strokeWidth={active ? 2 : 1.5} />
                {label}
              </Link>
            </li>
          );
        })}
      </ul>
    </m.nav>
  );
}

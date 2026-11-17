import { m } from "motion/react";
import { NavLink } from "react-router";
import { cn } from "cn";

import { SEGMENT_SPRING } from "@/lib/motion";

const LINKS = [
  { to: "/anfitrion/vehiculos", label: "Mis vehículos" },
  { to: "/anfitrion/reservas", label: "Reservas recibidas" },
];

// Cambia entre las dos secciones del anfitrión. En móvil es la única forma de pasar de una a
// otra (la barra inferior tiene una sola pestaña "Anfitrión").
export default function HostSubnav() {
  return (
    <nav
      aria-label="Secciones del anfitrión"
      className="inline-flex w-fit rounded-full bg-secondary p-1"
    >
      {LINKS.map(({ to, label }) => (
        <NavLink
          key={to}
          to={to}
          end
          className={({ isActive }) =>
            cn(
              "relative h-10 rounded-full px-4 text-[13.5px] leading-10 font-semibold outline-none focus-visible:ring-2 focus-visible:ring-ring",
              isActive
                ? "text-foreground"
                : "text-muted-foreground hover:text-foreground",
            )
          }
        >
          {({ isActive }) => (
            <>
              {isActive && (
                <m.span
                  layoutId="host-subnav"
                  aria-hidden="true"
                  transition={SEGMENT_SPRING}
                  className="absolute inset-0 rounded-full bg-background shadow-float"
                />
              )}
              <span className="relative">{label}</span>
            </>
          )}
        </NavLink>
      ))}
    </nav>
  );
}

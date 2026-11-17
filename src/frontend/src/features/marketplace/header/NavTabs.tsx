import { m } from "motion/react";
import { Link, useLocation } from "react-router";
import { cn } from "cn";

import { SEGMENT_SPRING } from "@/lib/motion";
import type { NavItem } from "./nav";

interface NavTabsProps {
  items: NavItem[];
  className?: string;
}

const UNDERLINE =
  "absolute inset-x-0 bottom-1.5 mx-auto h-0.5 w-10 rounded-full md:bottom-3.5 md:w-auto";

// Pestañas estilo Airbnb: ícono arriba, texto abajo y una línea bajo la activa que se desliza
// a la nueva al navegar. La pestaña activa sale de la URL.
export default function NavTabs({ items, className }: NavTabsProps) {
  const { pathname } = useLocation();

  return (
    <nav
      aria-label="Secciones de Wheelby"
      className={cn("flex h-full items-stretch", className)}
    >
      {items.map(({ to, label, icon: Icon, match }) => {
        const selected = match(pathname);
        return (
          <Link
            key={to}
            to={to}
            aria-current={selected ? "page" : undefined}
            className={cn(
              "group/tab relative flex flex-1 flex-col items-center justify-center gap-1.5 px-0.5 text-[12.5px] font-semibold transition-colors outline-none focus-visible:text-foreground md:flex-none",
              selected
                ? "text-foreground"
                : "text-muted-foreground hover:text-foreground",
            )}
          >
            <Icon
              className="size-6.5 transition-transform group-hover/tab:scale-105"
              strokeWidth={1.4}
            />
            {label}
            {selected ? (
              <m.span
                aria-hidden="true"
                layoutId="nav-underline"
                transition={SEGMENT_SPRING}
                className={cn(UNDERLINE, "bg-foreground")}
              />
            ) : (
              <span
                aria-hidden="true"
                className={cn(
                  UNDERLINE,
                  "scale-x-0 bg-border transition-transform duration-200 group-hover/tab:scale-x-100",
                )}
              />
            )}
          </Link>
        );
      })}
    </nav>
  );
}

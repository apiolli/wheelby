import {
  CalendarDays,
  Compass,
  Inbox,
  KeyRound,
  LayoutDashboard,
  UserRound,
  type LucideIcon,
} from "lucide-react";

export interface NavItem {
  to: string;
  label: string;
  icon: LucideIcon;
  // Rutas que también marcan la pestaña como activa.
  match: (pathname: string) => boolean;
}

// Pestañas del header (sustituyen a las categorías del prototipo: la API no las tiene). Llevan a
// las funciones reales del marketplace.
export const MEMBER_NAV: NavItem[] = [
  { to: "/", label: "Explorar", icon: Compass, match: (p) => p === "/" },
  {
    to: "/perfil/reservas",
    label: "Mis reservas",
    icon: CalendarDays,
    match: (p) => p.startsWith("/perfil/reservas"),
  },
  {
    to: "/anfitrion/reservas",
    label: "Solicitudes",
    icon: Inbox,
    match: (p) => p.startsWith("/anfitrion/reservas"),
  },
  {
    to: "/perfil/anfitrion",
    label: "Anfitrión",
    icon: KeyRound,
    match: (p) => p.startsWith("/perfil/anfitrion"),
  },
];

export const ADMIN_HEADER_NAV: NavItem[] = [
  {
    to: "/admin",
    label: "Panel",
    icon: LayoutDashboard,
    match: (p) => p.startsWith("/admin"),
  },
  {
    to: "/perfil",
    label: "Perfil",
    icon: UserRound,
    match: (p) => p.startsWith("/perfil"),
  },
];

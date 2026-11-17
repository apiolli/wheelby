import {
  CalendarDays,
  Car,
  Compass,
  Inbox,
  KeyRound,
  LayoutDashboard,
  UserRound,
  type LucideIcon,
} from "lucide-react";

import type { HostStatus } from "@/lib/permissions";

export interface NavItem {
  to: string;
  label: string;
  icon: LucideIcon;
  // Rutas que también marcan la pestaña como activa.
  match: (pathname: string) => boolean;
}

const explore: NavItem = {
  to: "/buscar",
  label: "Explorar",
  icon: Compass,
  match: (p) =>
    p === "/" || p.startsWith("/buscar") || p.startsWith("/vehiculos"),
};

const myReservations: NavItem = {
  to: "/perfil/reservas",
  label: "Mis reservas",
  icon: CalendarDays,
  match: (p) => p.startsWith("/perfil/reservas") || p.startsWith("/reservas/"),
};

// Pestañas del header en escritorio. El anfitrión verificado ve sus vehículos y las reservas
// recibidas; los demás, la invitación a serlo.
export function memberNav(status: HostStatus): NavItem[] {
  if (status === "verified")
    return [
      explore,
      myReservations,
      {
        to: "/anfitrion/vehiculos",
        label: "Mis vehículos",
        icon: Car,
        match: (p) => p.startsWith("/anfitrion/vehiculos"),
      },
      {
        to: "/anfitrion/reservas",
        label: "Reservas recibidas",
        icon: Inbox,
        match: (p) => p.startsWith("/anfitrion/reservas"),
      },
    ];
  return [
    explore,
    myReservations,
    {
      to: "/perfil/anfitrion",
      label: "Anfitrión",
      icon: KeyRound,
      match: (p) => p.startsWith("/perfil/anfitrion"),
    },
  ];
}

// Barra inferior en móvil: Explorar, Reservas y Perfil; el anfitrión verificado suma la suya.
export function mobileTabs(status: HostStatus): NavItem[] {
  const tabs: NavItem[] = [explore, { ...myReservations, label: "Reservas" }];
  if (status === "verified")
    tabs.push({
      to: "/anfitrion/vehiculos",
      label: "Anfitrión",
      icon: KeyRound,
      match: (p) => p.startsWith("/anfitrion"),
    });
  tabs.push({
    to: "/perfil",
    label: "Perfil",
    icon: UserRound,
    match: (p) => p.startsWith("/perfil") && !p.startsWith("/perfil/reservas"),
  });
  return tabs;
}

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

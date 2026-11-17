import {
  BadgeCheck,
  CalendarDays,
  FileText,
  LayoutDashboard,
  Users,
  type LucideIcon,
} from "lucide-react";

// Secciones del panel. Las futuras se añaden aquí con su ruta.
export const ADMIN_NAV: {
  to: string;
  label: string;
  icon: LucideIcon;
  end?: boolean;
}[] = [
  { to: "/admin", label: "Resumen", icon: LayoutDashboard, end: true },
  { to: "/admin/usuarios", label: "Usuarios", icon: Users },
  { to: "/admin/verificaciones", label: "Verificaciones", icon: BadgeCheck },
  { to: "/admin/reservas", label: "Reservas", icon: CalendarDays },
  { to: "/admin/documentos", label: "Documentos", icon: FileText },
];

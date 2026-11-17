import {
  BadgeCheck,
  CalendarDays,
  FileText,
  ShieldCheck,
  UserRound,
  type LucideIcon,
} from "lucide-react";

export interface ProfileSection {
  id: string;
  label: string;
  description: string;
  icon: LucideIcon;
  // Las secciones de alquiler son solo del rol Estándar: la API no deja al Administrador
  // reservar ni publicar.
  standardOnly?: boolean;
}

export const PROFILE_SECTIONS: ProfileSection[] = [
  {
    id: "cuenta",
    label: "Cuenta",
    description: "Tus datos y tu rol",
    icon: UserRound,
  },
  {
    id: "seguridad",
    label: "Seguridad",
    description: "Contraseña y sesión",
    icon: ShieldCheck,
  },
  {
    id: "reservas",
    label: "Mis reservas",
    description: "Tus viajes y su estado",
    icon: CalendarDays,
    standardOnly: true,
  },
  {
    id: "anfitrion",
    label: "Anfitrión",
    description: "Comparte tu vehículo",
    icon: BadgeCheck,
    standardOnly: true,
  },
  {
    id: "documentos",
    label: "Documentos",
    description: "Tus archivos",
    icon: FileText,
  },
];

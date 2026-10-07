import type { Role } from "@/mock/db";

// Aviso informativo (no es un error) que el login muestra sobre el formulario.
export type AuthNotice = "expired" | "password-reset";

// Estado de navegación entre las páginas de acceso: correo precargado y aviso.
export interface AuthRouteState {
  email?: string;
  notice?: AuthNotice;
}

// Datos de la cuenta que viajan con la sesión. Lo que puede cambiar mientras navegas
// (rol, estado de anfitrión…) se lee de la base de datos con useDb().
export interface User {
  id: string;
  name: string;
  email: string;
  role: Role;
}

export interface Session {
  user: User;
  expiresAt: number;
}

import type { Session, User } from "@/types/types";
import { createContext, useContext } from "react";

// checking: hay token y se está validando con GET /auth/me; no se muestra nada protegido.
export type SessionStatus = "checking" | "authenticated" | "anonymous";

// Por qué no hay sesión: decide adónde redirige ProtectedRoute y si el login muestra un aviso.
export type SessionEnd = "expired" | "logout" | null;

export interface SessionContextValue {
  status: SessionStatus;
  endReason: SessionEnd;
  session: Session | null;
  user: User | null;
  favorites: Set<string>;
  toggleFavorite: (id: string) => void;
  // Valida el token con GET /auth/me (en paralelo con `waitFor`, p. ej. una animación) y, si es
  // válido, inicia la sesión. Devuelve false si el backend lo rechaza.
  signIn: (token: string, waitFor?: Promise<unknown>) => Promise<boolean>;
  logout: () => void;
  showToast: (msg: string) => void;
}

export const SessionContext = createContext<SessionContextValue | null>(null);

export function useSession() {
  const ctx = useContext(SessionContext);
  if (!ctx) throw new Error("useSession debe usarse dentro de SessionProvider");
  return ctx;
}

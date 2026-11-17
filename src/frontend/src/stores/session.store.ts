import { isAxiosError } from "axios";
import { toast } from "sonner";
import { create } from "zustand";

import * as authApi from "@/api/auth.api";
import { onUnauthorized } from "@/api/wheelby-api";
import { firstName } from "@/lib/format";
import { clearToken, getToken, setToken } from "@/lib/token";
import type { User } from "@/types/types";

// checking: hay token y se está validando con GET /auth/me; no se muestra nada protegido.
export type SessionStatus = "checking" | "authenticated" | "anonymous";

// Por qué no hay sesión: decide adónde redirige ProtectedRoute y si el login muestra un aviso.
export type SessionEnd = "expired" | "logout" | null;

interface SessionState {
  status: SessionStatus;
  endReason: SessionEnd;
  user: User | null;
  expiresAt: number | null;
  // Valida el token con GET /auth/me (en paralelo con `waitFor`, p. ej. una animación) y, si es
  // válido, inicia la sesión. Devuelve false si el backend lo rechaza.
  signIn: (
    token: string,
    expiresAt: string,
    waitFor?: Promise<unknown>,
  ) => Promise<boolean>;
  // Sustituye el token de la sesión actual (p. ej. tras cambiar la contraseña).
  replaceToken: (token: string, expiresAt: string) => void;
  logout: () => void;
  expire: () => void;
}

let expiryTimer: ReturnType<typeof setTimeout> | undefined;

// El token guardado no trae su vencimiento a mano: se lee del propio JWT (claim exp).
function tokenExpiry(token: string): number | null {
  try {
    const payload = JSON.parse(
      atob(token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/")),
    );
    return typeof payload.exp === "number" ? payload.exp * 1000 : null;
  } catch {
    return null;
  }
}

function scheduleExpiry(expiresAt: number | null) {
  clearTimeout(expiryTimer);
  if (expiresAt === null) return;
  // setTimeout no admite más de ~24 días; el token dura 60 minutos.
  expiryTimer = setTimeout(
    () => useSession.getState().expire(),
    Math.max(0, expiresAt - Date.now()),
  );
}

export const showToast = (msg: string) => toast(msg, { duration: 2400 });

// Sesión de toda la app. Nace de un token validado con GET /auth/me; sin token válido no se
// muestra nada del negocio (ver ProtectedRoute).
export const useSession = create<SessionState>()((set, get) => ({
  status: getToken() ? "checking" : "anonymous",
  endReason: null,
  user: null,
  expiresAt: null,

  signIn: async (token, expiresAt, waitFor) => {
    try {
      const [user] = await Promise.all([authApi.getMe(token), waitFor]);
      const again = get().endReason === "expired";
      const exp = new Date(expiresAt).getTime();
      setToken(token);
      set({ status: "authenticated", endReason: null, user, expiresAt: exp });
      scheduleExpiry(exp);
      showToast(
        again
          ? `¡Hola de nuevo, ${firstName(user.fullName)}!`
          : `¡Hola, ${firstName(user.fullName)}!`,
      );
      return true;
    } catch {
      return false;
    }
  },

  replaceToken: (token, expiresAt) => {
    const exp = new Date(expiresAt).getTime();
    setToken(token);
    set({ expiresAt: exp });
    scheduleExpiry(exp);
  },

  logout: () => {
    // Se avisa al backend sin esperar: la sesión local se cierra de inmediato.
    authApi.logout().catch(() => {});
    clearToken();
    clearTimeout(expiryTimer);
    set({
      status: "anonymous",
      endReason: "logout",
      user: null,
      expiresAt: null,
    });
    showToast("Cerraste sesión");
  },

  // 401 o vencimiento: se borra el token y ProtectedRoute lleva al login con el aviso.
  expire: () => {
    if (get().status === "anonymous") return;
    clearToken();
    clearTimeout(expiryTimer);
    set({
      status: "anonymous",
      endReason: "expired",
      user: null,
      expiresAt: null,
    });
  },
}));

export const isAdmin = (user: User | null) => user?.role === "Administrator";

// Al cargar con un token guardado, se valida antes de mostrar nada protegido.
function bootstrap() {
  const token = getToken();
  if (!token) return;
  const exp = tokenExpiry(token);
  if (exp !== null && exp <= Date.now()) {
    useSession.getState().expire();
    return;
  }
  authApi
    .getMe()
    .then((user) => {
      useSession.setState({ status: "authenticated", user, expiresAt: exp });
      scheduleExpiry(exp);
    })
    .catch((error) => {
      // Token rechazado: sesión expirada. Sin conexión: al login sin aviso, conservando el token
      // para que una recarga lo intente de nuevo.
      if (isAxiosError(error) && error.response?.status === 401)
        useSession.getState().expire();
      else useSession.setState({ status: "anonymous", endReason: null });
    });
}

// Cualquier llamada con sesión que reciba 401 la cierra.
onUnauthorized(() => useSession.getState().expire());
bootstrap();

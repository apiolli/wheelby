import type { Session } from "@/types/types";
import { useEffect, useState, type ReactNode } from "react";
import { toast } from "sonner";
import { SessionContext, type SessionEnd } from "./session.store";
import { clearToken, getToken, onUnauthorized, setToken } from "@/mock/token";
import { getMe } from "@/mock/auth";
import { firstName } from "@/lib/format";

type State =
  | { status: "checking" }
  | { status: "authenticated"; session: Session }
  | { status: "anonymous"; reason: SessionEnd };

const NO_FAVORITES = new Set<string>();

// Sesión y favoritos de toda la app. La sesión nace de un token validado con GET /auth/me; sin
// token válido no se muestra nada del negocio (ver ProtectedRoute).
export default function SessionProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<State>(() =>
    getToken() ? { status: "checking" } : { status: "anonymous", reason: null },
  );
  // Los favoritos pertenecen a una cuenta: si entra otra, empiezan vacíos.
  const [favorites, setFavorites] = useState(() => ({
    owner: null as string | null,
    ids: new Set<string>(),
  }));
  const session = state.status === "authenticated" ? state.session : null;
  const user = session?.user ?? null;

  const showToast = (msg: string) => {
    toast(msg, { duration: 2400 });
  };

  // 401 o vencimiento: se borra el token y ProtectedRoute lleva al login con el aviso.
  const expire = () => {
    clearToken();
    setState({ status: "anonymous", reason: "expired" });
  };

  // Al cargar con un token guardado, se valida antes de mostrar nada protegido.
  useEffect(() => {
    const token = getToken();
    if (!token) return;
    let cancelled = false;
    getMe(token).then((me) => {
      if (cancelled) return;
      if (me.ok) {
        setFavorites((f) =>
          f.owner === me.user.id ? f : { owner: me.user.id, ids: new Set() },
        );
        setState({
          status: "authenticated",
          session: { user: me.user, expiresAt: me.expiresAt },
        });
      } else expire();
    });
    return () => {
      cancelled = true;
    };
  }, []);

  // Cualquier llamada autenticada que reciba 401 cierra la sesión.
  useEffect(() => onUnauthorized(expire), []);

  // El token vence a los 60 minutos aunque no haya llamadas.
  useEffect(() => {
    if (state.status !== "authenticated") return;
    const t = setTimeout(expire, state.session.expiresAt - Date.now());
    return () => clearTimeout(t);
  }, [state]);

  // #sesion-expirada simula un 401 con la sesión iniciada (útil para revisar el diseño).
  useEffect(() => {
    const onHash = () => {
      if (window.location.hash === "#sesion-expirada" && getToken()) expire();
    };
    window.addEventListener("hashchange", onHash);
    return () => window.removeEventListener("hashchange", onHash);
  }, []);

  const signIn = async (token: string, waitFor?: Promise<unknown>) => {
    const [me] = await Promise.all([getMe(token), waitFor]);
    if (!me.ok) return false;
    setToken(token);
    setFavorites((f) =>
      f.owner === me.user.id ? f : { owner: me.user.id, ids: new Set() },
    );
    const again = state.status === "anonymous" && state.reason === "expired";
    setState({
      status: "authenticated",
      session: { user: me.user, expiresAt: me.expiresAt },
    });
    showToast(
      again
        ? `¡Hola de nuevo, ${firstName(me.user.name)}!`
        : `¡Hola, ${firstName(me.user.name)}!`,
    );
    return true;
  };

  const logout = () => {
    clearToken();
    setFavorites({ owner: null, ids: new Set() });
    setState({ status: "anonymous", reason: "logout" });
    showToast("Cerraste sesión");
  };

  const toggleFavorite = (id: string) => {
    const saved = favorites.ids.has(id);
    setFavorites((f) => {
      const ids = new Set(f.ids);
      if (ids.has(id)) ids.delete(id);
      else ids.add(id);
      return { ...f, ids };
    });
    showToast(saved ? "Eliminado de favoritos" : "Guardado en favoritos");
  };

  return (
    <SessionContext
      value={{
        status: state.status,
        endReason: state.status === "anonymous" ? state.reason : null,
        session,
        user,
        favorites:
          user && favorites.owner === user.id ? favorites.ids : NO_FAVORITES,
        toggleFavorite,
        signIn,
        logout,
        showToast,
      }}
    >
      {children}
    </SessionContext>
  );
}

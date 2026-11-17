import {
  useEffect,
  useEffectEvent,
  useRef,
  useState,
  type FormEvent,
} from "react";

import * as authApi from "@/api/auth.api";
import { toApiError } from "@/api/errors";
import { fieldErrors, loginSchema } from "@/lib/schemas";
import { useSession } from "@/stores/session.store";
import { useAuthMotion } from "../../shared/motion.context";

type LoginError =
  | { kind: "invalid" | "inactive" | "disabled" }
  | { kind: "locked"; until: number }
  | { kind: "other"; message: string };

const MESSAGES = {
  invalid:
    "El correo o la contraseña no son correctos. Revisa tus datos e inténtalo de nuevo.",
  inactive: "Tu cuenta aún no está activa. Revisa tu correo para activarla.",
  disabled: "Tu cuenta está desactivada. Escríbenos si crees que es un error.",
  locked: "Demasiados intentos. Intenta de nuevo en unos minutos.",
};

// La API no dice cuánto falta para que venza el bloqueo: el formulario se reactiva pasado este
// tiempo y, si sigue bloqueada, el backend lo vuelve a indicar.
const LOCK_PAUSE_MS = 60_000;

// El backend responde 403 con el motivo en el texto para cuenta inactiva, bloqueada o desactivada.
function classify(status: number | null, message: string): LoginError {
  if (status === 401) return { kind: "invalid" };
  if (status === 403) {
    const text = message.toLowerCase();
    if (text.includes("bloqueada"))
      return { kind: "locked", until: Date.now() + LOCK_PAUSE_MS };
    if (text.includes("desactivada")) return { kind: "disabled" };
    if (text.includes("no está activa")) return { kind: "inactive" };
  }
  return { kind: "other", message };
}

export const loginErrorMessage = (error: LoginError) =>
  error.kind === "other" ? error.message : MESSAGES[error.kind];

export const useLogin = (initialEmail: string) => {
  const [email, setEmail] = useState(initialEmail);
  const [password, setPassword] = useState("");
  const [touched, setTouched] = useState(false);
  const [error, setError] = useState<LoginError | null>(null);
  const [loading, setLoading] = useState(false);
  const [resending, setResending] = useState(false);
  const [resent, setResent] = useState(false);
  const errorRef = useRef<HTMLDivElement>(null);
  const { swap, shake, stopScene, resumeScene, celebrate } = useAuthMotion();
  const signIn = useSession((s) => s.signIn);

  const invalid = touched ? fieldErrors(loginSchema, { email, password }) : {};
  const lockedUntil = error?.kind === "locked" ? error.until : null;
  // La escena del panel no forma parte del estado del formulario: se avisa sin ser dependencia.
  const pauseScene = useEffectEvent(() => stopScene());
  const continueScene = useEffectEvent(() => resumeScene());
  const locked = lockedUntil !== null;

  useEffect(() => {
    if (lockedUntil === null) return;
    // El botón enfocado queda deshabilitado: llevamos el foco al aviso para no perderlo.
    errorRef.current?.focus();
    // El vehículo de la escena se detiene mientras dure el bloqueo.
    pauseScene();
    const t = setTimeout(() => setError(null), lockedUntil - Date.now());
    return () => {
      clearTimeout(t);
      continueScene();
    };
  }, [lockedUntil]);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setTouched(true);
    if (!loginSchema.safeParse({ email, password }).success) return;
    setError(null);
    setLoading(true);
    try {
      const res = await authApi.login(email, password);
      // GET /auth/me valida el token mientras corre la animación de salida; la sesión (y con ella
      // la navegación al catálogo) solo empieza cuando ambas terminan.
      if (await signIn(res.accessToken, res.expiresAt, celebrate())) return;
      setLoading(false);
      setError({ kind: "invalid" });
    } catch (err) {
      const { status, message } = toApiError(err);
      const next = classify(status, message);
      setLoading(false);
      setError(next);
      // Solo el error de credenciales sacude el formulario; el bloqueo no es un error de tecleo.
      if (next.kind === "invalid") shake();
    }
  };

  const resend = async () => {
    setResending(true);
    // La respuesta es la misma exista o no la cuenta; un fallo de red no cambia el mensaje.
    await authApi.resendActivation(email).catch(() => {});
    swap(() => {
      setResending(false);
      setResent(true);
    });
  };

  return {
    loading,
    resending,
    locked,
    resent,
    errorRef,
    error,
    invalid,
    email,
    password,
    setEmail,
    setPassword,
    resend,
    submit,
    swap,
    setResent,
    setError,
  };
};
